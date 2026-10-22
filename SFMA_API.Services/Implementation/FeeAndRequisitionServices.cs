using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class FeeService : IFeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISchoolNotificationService _notificationService;

        public FeeService(IUnitOfWork unitOfWork, IMapper mapper, ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<FeeScheduleResponse?> GetFeeSchedule(Guid termId, Guid? classSectionId)
        {
            var scheduleRepo = _unitOfWork.GetRepository<FeeSchedule>();
            var schedule = await scheduleRepo.GetSingleByAsync(
                f => f.AcademicTermId == termId && (classSectionId.HasValue ? (f.ClassSectionId == classSectionId || f.ClassSectionId == null) : f.ClassSectionId == null),
                orderBy: q => q.OrderByDescending(f => f.ClassSectionId.HasValue),
                include: q => q.Include(f => f.FeeItems));

            if (schedule == null) return null;

            var response = _mapper.Map<FeeScheduleResponse>(schedule);
            response.FeeItems = response.FeeItems.OrderBy(i => i.SortOrder).ToList();
            return response;
        }

        public async Task<FeeScheduleResponse> UpsertFeeSchedule(UpsertFeeScheduleRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.AcademicTermId == Guid.Empty)
                throw new ArgumentException("AcademicTermId is required.");

            if (request.FeeItems == null || !request.FeeItems.Any())
                throw new ArgumentException("At least one fee item is required to configure a fee schedule.");

            if (request.FeeItems.Any(i => string.IsNullOrWhiteSpace(i.Name) || i.Amount < 0))
                throw new ArgumentException("Each fee item must have a valid name and a non-negative amount.");

            var term = await _unitOfWork.GetRepository<AcademicTerm>().GetByIdAsync(request.AcademicTermId);
            if (term == null)
                throw new KeyNotFoundException("Academic term not found.");

            if (request.ClassSectionId.HasValue && request.ClassSectionId.Value != Guid.Empty)
            {
                await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassSectionId.Value, request.ClassName, request.AcademicTermId);
            }

            var scheduleRepo = _unitOfWork.GetRepository<FeeSchedule>();
            var itemRepo = _unitOfWork.GetRepository<FeeItem>();

            var existingSchedule = await scheduleRepo.GetSingleByAsync(
                f => f.AcademicTermId == request.AcademicTermId && f.ClassSectionId == request.ClassSectionId);

            decimal totalAmount = request.FeeItems.Sum(i => i.Amount);
            Guid scheduleId;

            if (existingSchedule == null)
            {
                var newSchedule = new FeeSchedule
                {
                    Id = Guid.NewGuid(),
                    AcademicTermId = request.AcademicTermId,
                    ClassSectionId = request.ClassSectionId,
                    TotalAmount = totalAmount,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                await scheduleRepo.AddAsync(newSchedule);
                scheduleId = newSchedule.Id;
            }
            else
            {
                existingSchedule.TotalAmount = totalAmount;
                existingSchedule.UpdatedAt = DateTime.UtcNow;
                await scheduleRepo.UpdateAsync(existingSchedule);
                scheduleId = existingSchedule.Id;

                await itemRepo.DeleteRangeAsync(i => i.FeeScheduleId == scheduleId);
            }

            var newItems = request.FeeItems
                .Select((item, idx) => new FeeItem
                {
                    Id = Guid.NewGuid(),
                    FeeScheduleId = scheduleId,
                    Name = item.Name.Trim(),
                    Amount = item.Amount,
                    SortOrder = item.SortOrder > 0 ? item.SortOrder : (short)(idx + 1),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                })
                .OrderBy(i => i.SortOrder)
                .ToList();

            await itemRepo.AddRangeAsync(newItems);
            await _unitOfWork.SaveChangesAsync();

            return new FeeScheduleResponse
            {
                Id = scheduleId,
                AcademicTermId = request.AcademicTermId,
                ClassSectionId = request.ClassSectionId,
                TotalAmount = totalAmount,
                FeeItems = _mapper.Map<List<FeeItemResponse>>(newItems)
            };
        }

        public async Task<PagedResponse<FeeTransactionResponse>> GetFeeLedger(FeeTransactionStatus? status, RequestParameters parameters)
        {
            var query = _unitOfWork.GetRepository<FeeTransaction>().GetQueryable(
                include: q => q.Include(f => f.Student).Include(f => f.VerifiedBy).ThenInclude(v => v!.User));

            if (status.HasValue) query = query.Where(f => f.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim().ToLower();
                query = query.Where(f => f.Student.FullName.ToLower().Contains(term) || f.Student.StudentCode.ToLower().Contains(term) || (f.BankTellerRef != null && f.BankTellerRef.ToLower().Contains(term)));
            }

            var count = await query.CountAsync();
            var skip = (parameters.PageNumber - 1) * parameters.PageSize;
            var items = await query.OrderByDescending(f => f.PaymentDate).Skip(skip).Take(parameters.PageSize).ToListAsync();

            return new PagedResponse<FeeTransactionResponse>
            {
                MetaData = new MetaData
                {
                    TotalCount = count,
                    CurrentPage = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = (int)Math.Ceiling(count / (double)parameters.PageSize)
                },
                Items = _mapper.Map<IEnumerable<FeeTransactionResponse>>(items)
            };
        }

        public async Task<StudentFeeSummaryResponse> GetStudentFeeSummary(Guid studentId, ClaimsPrincipal currentUser)
        {
            var student = await _unitOfWork.GetRepository<Student>().GetByIdAsync(studentId);
            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive)
                ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

            var feeSchedule = await _unitOfWork.GetRepository<FeeSchedule>().GetSingleByAsync(
                f => f.AcademicTermId == activeTerm!.Id && (f.ClassSectionId == student.ClassSectionId || f.ClassSectionId == null),
                orderBy: q => q.OrderByDescending(f => f.ClassSectionId.HasValue),
                include: q => q.Include(f => f.FeeItems));

            // No silent fallback — if there is no fee schedule the data would be wrong.
            // Administrators must configure a fee schedule for the active term before summaries can be served.
            if (feeSchedule == null)
            {
                throw new KeyNotFoundException(
                    "No fee schedule has been configured for this student's class and the current active term. " +
                    "Please set up a fee schedule before viewing student fee summaries.");
            }

            decimal totalBilled = feeSchedule.TotalAmount;

            var transactions = await _unitOfWork.GetRepository<FeeTransaction>().GetByAsync(
                f => f.StudentId == studentId,
                include: q => q.Include(f => f.Student).Include(f => f.VerifiedBy).ThenInclude(v => v!.User),
                orderBy: q => q.OrderByDescending(f => f.PaymentDate));

            decimal totalPaid = transactions
                .Where(t => t.Status == FeeTransactionStatus.Verified || t.Status == FeeTransactionStatus.Cleared)
                .Sum(t => t.AmountPaid);

            decimal balance = Math.Max(0, totalBilled - totalPaid);

            return new StudentFeeSummaryResponse
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                TotalBilled = totalBilled,
                TotalPaid = totalPaid,
                Balance = balance,
                FeeItems = _mapper.Map<List<FeeItemResponse>>(feeSchedule.FeeItems.OrderBy(i => i.SortOrder).ToList()),
                Transactions = _mapper.Map<List<FeeTransactionResponse>>(transactions)
            };
        }

        public async Task<FeeTransactionResponse> PostTeller(PostTellerRequest request, ClaimsPrincipal currentUser)
        {
            var student = await _unitOfWork.GetRepository<Student>().GetByIdAsync(request.StudentId);
            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive)
                ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

            if (activeTerm == null)
            {
                throw new InvalidOperationException("No active academic term found for fee transaction.");
            }

            var transaction = new FeeTransaction
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                AcademicTermId = request.AcademicTermId ?? activeTerm.Id,
                AmountPaid = request.Amount,
                PaymentDate = request.PaymentDate,
                // PaymentMethod is now supplied by the caller — no longer hardcoded to "Bank Transfer"
                PaymentMethod = request.PaymentMethod,
                BankTellerRef = request.BankTellerRef,
                BankName = request.BankName,
                AttachmentFile = request.AttachmentFile,
                Status = FeeTransactionStatus.PendingReview,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<FeeTransaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var res = _mapper.Map<FeeTransactionResponse>(transaction);
            res.StudentName = student.FullName;
            res.StudentCode = student.StudentCode;
            return res;
        }

        public async Task<FeeTransactionResponse> VerifyTeller(Guid id, VerifyTellerRequest request, ClaimsPrincipal currentUser)
        {
            var transaction = await _unitOfWork.GetRepository<FeeTransaction>().GetSingleByAsync(
                f => f.Id == id,
                include: q => q.Include(f => f.Student));

            if (transaction == null)
            {
                throw new KeyNotFoundException("Fee transaction not found");
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User))
                ?? await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => true);

            if (staff == null)
            {
                throw new InvalidOperationException("No staff account found to verify fee transaction.");
            }

            if (request.Status.Equals("cleared", StringComparison.OrdinalIgnoreCase))
                transaction.Status = FeeTransactionStatus.Cleared;
            else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                transaction.Status = FeeTransactionStatus.Rejected;
            else
                transaction.Status = FeeTransactionStatus.Verified;

            transaction.VerifiedById = staff.Id;
            if (request.Notes != null) transaction.Notes = request.Notes;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<FeeTransaction>().UpdateAsync(transaction);

            // Auto-generate official Receipt if verified or cleared
            if (transaction.Status == FeeTransactionStatus.Verified || transaction.Status == FeeTransactionStatus.Cleared)
            {
                var existingReceipt = await _unitOfWork.GetRepository<Receipt>().GetSingleByAsync(r => r.FeeTransactionId == transaction.Id);
                if (existingReceipt == null)
                {
                    var totalReceipts = await _unitOfWork.GetRepository<Receipt>().CountAsync();
                    // Year is dynamic — not hardcoded to 2026
                    string receiptCode = $"REC-{DateTime.UtcNow.Year}-{(totalReceipts + 1):D4}";

                    var receipt = new Receipt
                    {
                        Id = Guid.NewGuid(),
                        ReceiptCode = receiptCode,
                        FeeTransactionId = transaction.Id,
                        IssuedAt = DateTime.UtcNow,
                        VerifiedById = staff.Id,
                        PdfUrl = $"/receipts/{receiptCode}.pdf"
                    };
                    await _unitOfWork.GetRepository<Receipt>().AddAsync(receipt);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Dispatch payment verification/rejection confirmation to parent
            var primaryParent = await _unitOfWork.GetRepository<StudentParent>()
                .GetQueryable(sp => sp.StudentId == transaction.StudentId && sp.IsPrimary)
                .Include(sp => sp.Parent)
                .Select(sp => sp.Parent)
                .FirstOrDefaultAsync();

            if (primaryParent == null)
            {
                primaryParent = await _unitOfWork.GetRepository<StudentParent>()
                    .GetQueryable(sp => sp.StudentId == transaction.StudentId)
                    .Include(sp => sp.Parent)
                    .Select(sp => sp.Parent)
                    .FirstOrDefaultAsync();
            }

            if (primaryParent != null)
            {
                string receiptCode = "";
                if (transaction.Status == FeeTransactionStatus.Verified || transaction.Status == FeeTransactionStatus.Cleared)
                {
                    var receiptForNotification = await _unitOfWork.GetRepository<Receipt>()
                        .GetSingleByAsync(r => r.FeeTransactionId == transaction.Id);
                    receiptCode = receiptForNotification?.ReceiptCode ?? "";
                }

                string statusLabel = transaction.Status switch
                {
                    FeeTransactionStatus.Verified => "Verified",
                    FeeTransactionStatus.Cleared => "Cleared",
                    FeeTransactionStatus.Rejected => "Rejected",
                    _ => transaction.Status.ToString()
                };

                _ = Task.Run(() => _notificationService.SendPaymentVerificationConfirmationAsync(
                    primaryParent.FullName,
                    primaryParent.Email,
                    primaryParent.Phone,
                    transaction.Student?.FullName ?? "Scholar",
                    transaction.Student?.StudentCode ?? "",
                    transaction.AmountPaid,
                    receiptCode,
                    transaction.PaymentMethod,
                    statusLabel));
            }

            var response = _mapper.Map<FeeTransactionResponse>(transaction);
            // Use actual verifier name; fall back to "Unknown" rather than a misleading role name
            response.VerifiedByName = staff?.User?.DisplayName ?? "Unknown";
            return response;
        }

        public async Task<IEnumerable<ReceiptResponse>> GetStudentReceipts(Guid studentId, ClaimsPrincipal currentUser)
        {
            var receipts = await _unitOfWork.GetRepository<Receipt>().GetByAsync(
                r => r.FeeTransaction.StudentId == studentId,
                include: q => q.Include(r => r.FeeTransaction).ThenInclude(f => f.Student)
                               .Include(r => r.VerifiedBy).ThenInclude(v => v.User));

            return _mapper.Map<IEnumerable<ReceiptResponse>>(receipts);
        }

        public async Task<ReceiptResponse?> GetReceiptPdf(Guid receiptId, ClaimsPrincipal currentUser)
        {
            var receipt = await _unitOfWork.GetRepository<Receipt>().GetSingleByAsync(
                r => r.Id == receiptId,
                include: q => q.Include(r => r.FeeTransaction).ThenInclude(f => f.Student)
                               .Include(r => r.VerifiedBy).ThenInclude(v => v.User));

            return receipt != null ? _mapper.Map<ReceiptResponse>(receipt) : null;
        }

        public async Task<bool> QueuePaymentReminders(QueuePaymentRemindersRequest request)
        {
            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>()
                .GetSingleByAsync(t => t.IsActive);
            if (activeTerm == null) return false;

            string termLabel = $"Term {activeTerm.TermNumber} {activeTerm.SessionName}";

            var students = await _unitOfWork.GetRepository<Student>()
                .GetQueryable(include: q => q
                    .Include(s => s.StudentParents).ThenInclude(sp => sp.Parent)
                    .Include(s => s.FeeTransactions))
                .ToListAsync();

            var schedules = await _unitOfWork.GetRepository<FeeSchedule>()
                .GetByAsync(f => f.AcademicTermId == activeTerm.Id,
                    include: q => q.Include(f => f.FeeItems));

            int sentCount = 0;
            foreach (var student in students)
            {
                decimal totalBilled = schedules
                    .Where(s => s.ClassSectionId == null || s.ClassSectionId == student.ClassSectionId)
                    .Sum(s => s.TotalAmount);

                decimal totalPaid = student.FeeTransactions
                    .Where(t => t.AcademicTermId == activeTerm.Id &&
                        (t.Status == FeeTransactionStatus.Verified ||
                         t.Status == FeeTransactionStatus.Cleared))
                    .Sum(t => t.AmountPaid);

                decimal balance = totalBilled - totalPaid;
                if (balance <= 0) continue;

                var primaryParent = student.StudentParents
                    .Where(sp => sp.IsPrimary).Select(sp => sp.Parent).FirstOrDefault()
                    ?? student.StudentParents.Select(sp => sp.Parent).FirstOrDefault();

                if (primaryParent == null) continue;

                _ = Task.Run(() => _notificationService.SendFeePaymentReminderAsync(
                    primaryParent.FullName,
                    primaryParent.Email,
                    primaryParent.Phone,
                    student.FullName,
                    student.StudentCode,
                    balance,
                    termLabel));

                sentCount++;
            }

            return sentCount > 0;
        }

        public async Task<IEnumerable<BankAccountResponse>> GetBankAccounts()
        {
            var accounts = await _unitOfWork.GetRepository<BankAccount>().GetByAsync(b => b.IsActive && b.Active);
            return _mapper.Map<IEnumerable<BankAccountResponse>>(accounts);
        }

        public async Task<BankAccountResponse> UpsertBankAccount(UpsertBankAccountRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(request.AccountName) || string.IsNullOrWhiteSpace(request.AccountNumber))
                throw new ArgumentException("BankName, AccountName, and AccountNumber are required.");

            var repo = _unitOfWork.GetRepository<BankAccount>();
            string normalizedAccountNumber = request.AccountNumber.Trim();

            BankAccount? account = null;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                account = await repo.GetByIdAsync(request.Id.Value);
                if (account == null)
                    throw new KeyNotFoundException("Bank account not found.");
            }
            else
            {
                account = await repo.GetSingleByAsync(b => b.AccountNumber == normalizedAccountNumber);
            }

            if (account == null)
            {
                account = new BankAccount
                {
                    Id = Guid.NewGuid(),
                    BankName = request.BankName.Trim(),
                    AccountName = request.AccountName.Trim(),
                    AccountNumber = normalizedAccountNumber,
                    SortCode = request.SortCode?.Trim() ?? string.Empty,
                    IsActive = request.IsActive,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };
                await repo.AddAsync(account);
            }
            else
            {
                account.BankName = request.BankName.Trim();
                account.AccountName = request.AccountName.Trim();
                account.AccountNumber = normalizedAccountNumber;
                account.SortCode = request.SortCode?.Trim() ?? string.Empty;
                account.IsActive = request.IsActive;
                account.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(account);
            }

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<BankAccountResponse>(account);
        }

        public async Task<StudentFeeSummaryResponse> GetStatementOfAccount(Guid studentId, ClaimsPrincipal currentUser)
        {
            return await GetStudentFeeSummary(studentId, currentUser);
        }
    }

    public class RequisitionService : IRequisitionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISchoolNotificationService _notificationService;

        public RequisitionService(IUnitOfWork unitOfWork, IMapper mapper, ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<PagedResponse<RequisitionResponse>> GetRequisitions(RequisitionStatus? status, string? department, RequestParameters parameters, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<Requisition>().GetQueryable(
                include: q => q.Include(r => r.RequestedBy).ThenInclude(s => s.User)
                               .Include(r => r.ApprovalStages).ThenInclude(a => a.ActedBy).ThenInclude(ab => ab!.User));

            if (status.HasValue) query = query.Where(r => r.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(department)) query = query.Where(r => r.Department == department);

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            if (roles.Contains("teacher") && !roles.Contains("super_admin") && !roles.Contains("academic_head") && !roles.Contains("financial_head"))
            {
                query = query.Where(r => r.RequestedBy.UserId == userId);
            }

            var count = await query.CountAsync();
            var skip = (parameters.PageNumber - 1) * parameters.PageSize;
            var items = await query.OrderByDescending(r => r.CreatedAt).Skip(skip).Take(parameters.PageSize).ToListAsync();

            return new PagedResponse<RequisitionResponse>
            {
                MetaData = new MetaData
                {
                    TotalCount = count,
                    CurrentPage = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = (int)Math.Ceiling(count / (double)parameters.PageSize)
                },
                Items = _mapper.Map<IEnumerable<RequisitionResponse>>(items)
            };
        }

        public async Task<RequisitionResponse?> GetRequisitionById(Guid id, ClaimsPrincipal currentUser)
        {
            var req = await _unitOfWork.GetRepository<Requisition>().GetSingleByAsync(
                r => r.Id == id,
                include: q => q.Include(r => r.RequestedBy).ThenInclude(s => s.User)
                               .Include(r => r.ApprovalStages).ThenInclude(a => a.ActedBy).ThenInclude(ab => ab!.User));

            return req != null ? _mapper.Map<RequisitionResponse>(req) : null;
        }

        public async Task<RequisitionResponse> CreateRequisition(CreateRequisitionRequest request, ClaimsPrincipal currentUser)
        {
            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User))
                ?? await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => true);

            if (staff == null)
            {
                throw new InvalidOperationException("No staff account found to create requisition.");
            }

            var totalReqs = await _unitOfWork.GetRepository<Requisition>().CountAsync();
            // Year is dynamic — not hardcoded to 2026
            string reqCode = $"REQ-{DateTime.UtcNow.Year}-{(totalReqs + 1):D4}";

            var requisition = new Requisition
            {
                Id = Guid.NewGuid(),
                RequisitionCode = reqCode,
                Title = request.Title,
                Department = request.Department,
                Description = request.Description,
                Amount = request.Amount,
                Priority = request.Priority,
                RequestedById = staff.Id,
                Status = RequisitionStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Requisition>().AddAsync(requisition);

            // Initial Approval Stage: Academic Head
            var stage = new ApprovalStage
            {
                Id = Guid.NewGuid(),
                RequisitionId = requisition.Id,
                Stage = ApprovalStageType.AcademicHead,
                Action = ApprovalAction.Pending,
                ActedById = null,
                ActedAt = null
            };
            await _unitOfWork.GetRepository<ApprovalStage>().AddAsync(stage);

            await _unitOfWork.SaveChangesAsync();

            var res = _mapper.Map<RequisitionResponse>(requisition);
            // Use actual name; fall back to "Unknown" rather than a misleading role name
            res.RequestedByName = staff?.User?.DisplayName ?? "Unknown";
            return res;
        }

        public async Task<RequisitionResponse> ApproveRequisition(Guid id, ApproveRequisitionRequest request, ClaimsPrincipal currentUser)
        {
            var requisition = await _unitOfWork.GetRepository<Requisition>().GetSingleByAsync(
                r => r.Id == id,
                include: q => q.Include(r => r.RequestedBy).ThenInclude(s => s.User)
                               .Include(r => r.ApprovalStages));

            if (requisition == null)
            {
                throw new KeyNotFoundException("Requisition not found");
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User));
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            var currentStage = requisition.ApprovalStages.OrderByDescending(s => s.CreatedAt).FirstOrDefault(s => s.Action == ApprovalAction.Pending);

            if (roles.Contains("academic_head"))
            {
                if (currentStage != null)
                {
                    currentStage.Action = ApprovalAction.Approved;
                    currentStage.ActedById = staff?.Id;
                    currentStage.ActedAt = DateTime.UtcNow;
                    currentStage.Notes = request.Notes;
                    await _unitOfWork.GetRepository<ApprovalStage>().UpdateAsync(currentStage);
                }

                // Advance to Financial Head
                var nextStage = new ApprovalStage
                {
                    Id = Guid.NewGuid(),
                    RequisitionId = requisition.Id,
                    Stage = ApprovalStageType.FinancialHead,
                    Action = ApprovalAction.Pending
                };
                await _unitOfWork.GetRepository<ApprovalStage>().AddAsync(nextStage);
                requisition.Status = RequisitionStatus.InReview;
            }
            else if (roles.Contains("financial_head") || roles.Contains("super_admin"))
            {
                if (currentStage != null)
                {
                    currentStage.Action = ApprovalAction.Approved;
                    currentStage.ActedById = staff?.Id;
                    currentStage.ActedAt = DateTime.UtcNow;
                    currentStage.Notes = request.Notes;
                    await _unitOfWork.GetRepository<ApprovalStage>().UpdateAsync(currentStage);
                }

                // Strict 2-tier workflow concludes at Financial Head approval
                requisition.Status = RequisitionStatus.Approved;
            }

            requisition.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Requisition>().UpdateAsync(requisition);
            await _unitOfWork.SaveChangesAsync();

            // Notify the requester about the approval status change
            if (requisition.RequestedBy?.User?.Email != null)
            {
                string statusLabel = requisition.Status.ToString();
                _ = Task.Run(() => _notificationService.SendRequisitionStatusUpdateAsync(
                    requisition.RequestedBy.User.DisplayName,
                    requisition.RequestedBy.User.Email,
                    requisition.RequisitionCode,
                    requisition.Title,
                    statusLabel,
                    staff?.User?.DisplayName ?? "Approver",
                    roles.FirstOrDefault() ?? "Supervisor",
                    request.Notes));
            }

            return _mapper.Map<RequisitionResponse>(requisition);
        }

        public async Task<RequisitionResponse> QueryRequisition(Guid id, QueryRequisitionRequest request, ClaimsPrincipal currentUser)
        {
            var requisition = await _unitOfWork.GetRepository<Requisition>().GetSingleByAsync(
                r => r.Id == id,
                include: q => q.Include(r => r.RequestedBy).ThenInclude(s => s.User)
                               .Include(r => r.ApprovalStages));

            if (requisition == null)
            {
                throw new KeyNotFoundException("Requisition not found");
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User));
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            var currentStage = requisition.ApprovalStages.OrderByDescending(s => s.CreatedAt).FirstOrDefault(s => s.Action == ApprovalAction.Pending);
            if (currentStage != null)
            {
                currentStage.Action = ApprovalAction.Queried;
                currentStage.ActedById = staff?.Id;
                currentStage.ActedAt = DateTime.UtcNow;
                currentStage.Notes = request.Reason;
                await _unitOfWork.GetRepository<ApprovalStage>().UpdateAsync(currentStage);
            }

            requisition.Status = RequisitionStatus.Queried;
            requisition.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Requisition>().UpdateAsync(requisition);
            await _unitOfWork.SaveChangesAsync();

            // Notify the requester about the query/rejection
            if (requisition.RequestedBy?.User?.Email != null)
            {
                _ = Task.Run(() => _notificationService.SendRequisitionStatusUpdateAsync(
                    requisition.RequestedBy.User.DisplayName,
                    requisition.RequestedBy.User.Email,
                    requisition.RequisitionCode,
                    requisition.Title,
                    "Queried",
                    staff?.User?.DisplayName ?? "Reviewer",
                    roles.FirstOrDefault() ?? "Supervisor",
                    request.Reason));
            }

            return _mapper.Map<RequisitionResponse>(requisition);
        }

        public async Task<BudgetItemProgress> UpsertDepartmentBudget(UpsertDepartmentBudgetRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Department))
                throw new ArgumentException("Department name is required.");

            if (request.AllocatedAmount < 0)
                throw new ArgumentException("AllocatedAmount cannot be negative.");

            Guid termId;
            if (request.AcademicTermId.HasValue && request.AcademicTermId.Value != Guid.Empty)
            {
                termId = request.AcademicTermId.Value;
            }
            else
            {
                var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive && t.Active)
                    ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

                if (activeTerm == null)
                    throw new InvalidOperationException("No academic term is configured. Please create an academic session and term first.");

                termId = activeTerm.Id;
            }

            string normalizedDept = request.Department.Trim();
            var budgetRepo = _unitOfWork.GetRepository<DepartmentBudget>();
            var existing = await budgetRepo.GetSingleByAsync(
                b => b.AcademicTermId == termId && b.Department.ToLower() == normalizedDept.ToLower());

            if (existing == null)
            {
                existing = new DepartmentBudget
                {
                    Id = Guid.NewGuid(),
                    Department = normalizedDept,
                    AcademicTermId = termId,
                    AllocatedAmount = request.AllocatedAmount,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };
                await budgetRepo.AddAsync(existing);
            }
            else
            {
                existing.Department = normalizedDept;
                existing.AllocatedAmount = request.AllocatedAmount;
                existing.UpdatedAt = DateTime.UtcNow;
                await budgetRepo.UpdateAsync(existing);
            }

            await _unitOfWork.SaveChangesAsync();

            var actual = await _unitOfWork.GetRepository<Requisition>()
                .GetQueryable(r => r.Department.ToLower() == normalizedDept.ToLower() && (r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Disbursed))
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            return new BudgetItemProgress
            {
                Category = existing.Department,
                BudgetedAmount = existing.AllocatedAmount,
                ActualAmount = actual
            };
        }
    }
}

