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

        public FeeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FeeScheduleResponse?> GetFeeSchedule(Guid termId, Guid? classSectionId)
        {
            var scheduleRepo = _unitOfWork.GetRepository<FeeSchedule>();
            var schedule = await scheduleRepo.GetSingleByAsync(
                f => f.AcademicTermId == termId && (f.ClassSectionId == classSectionId || f.ClassSectionId == null),
                include: q => q.Include(f => f.FeeItems));

            return schedule != null ? _mapper.Map<FeeScheduleResponse>(schedule) : null;
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
                f => f.AcademicTermId == activeTerm!.Id && (f.ClassSectionId == student.ClassSectionId || f.ClassSectionId == null));

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

            var transaction = new FeeTransaction
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                AcademicTermId = request.AcademicTermId ?? activeTerm?.Id ?? Guid.Empty,
                AmountPaid = request.Amount,
                PaymentDate = request.PaymentDate,
                // PaymentMethod is now supplied by the caller — no longer hardcoded to "Bank Transfer"
                PaymentMethod = request.PaymentMethod,
                BankTellerRef = request.BankTellerRef,
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
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User));

            if (request.Status.Equals("cleared", StringComparison.OrdinalIgnoreCase))
                transaction.Status = FeeTransactionStatus.Cleared;
            else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                transaction.Status = FeeTransactionStatus.Rejected;
            else
                transaction.Status = FeeTransactionStatus.Verified;

            transaction.VerifiedById = staff?.Id;
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
                        VerifiedById = staff?.Id ?? Guid.Empty,
                        PdfUrl = $"/receipts/{receiptCode}.pdf"
                    };
                    await _unitOfWork.GetRepository<Receipt>().AddAsync(receipt);
                }
            }

            await _unitOfWork.SaveChangesAsync();

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

        public Task<bool> QueuePaymentReminders(QueuePaymentRemindersRequest request)
        {
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<BankAccountResponse>> GetBankAccounts()
        {
            var accounts = await _unitOfWork.GetRepository<BankAccount>().GetByAsync(b => b.IsActive && b.Active);
            return _mapper.Map<IEnumerable<BankAccountResponse>>(accounts);
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

        public RequisitionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
                RequestedById = staff?.Id ?? Guid.Empty,
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
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);
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
            else if (roles.Contains("financial_head"))
            {
                if (currentStage != null)
                {
                    currentStage.Action = ApprovalAction.Approved;
                    currentStage.ActedById = staff?.Id;
                    currentStage.ActedAt = DateTime.UtcNow;
                    currentStage.Notes = request.Notes;
                    await _unitOfWork.GetRepository<ApprovalStage>().UpdateAsync(currentStage);
                }

                // Advance to Super Admin
                var nextStage = new ApprovalStage
                {
                    Id = Guid.NewGuid(),
                    RequisitionId = requisition.Id,
                    Stage = ApprovalStageType.SuperAdmin,
                    Action = ApprovalAction.Pending
                };
                await _unitOfWork.GetRepository<ApprovalStage>().AddAsync(nextStage);
                requisition.Status = RequisitionStatus.InReview;
            }
            else if (roles.Contains("super_admin"))
            {
                if (currentStage != null)
                {
                    currentStage.Action = ApprovalAction.Approved;
                    currentStage.ActedById = staff?.Id;
                    currentStage.ActedAt = DateTime.UtcNow;
                    currentStage.Notes = request.Notes;
                    await _unitOfWork.GetRepository<ApprovalStage>().UpdateAsync(currentStage);
                }
                requisition.Status = RequisitionStatus.Disbursed;
            }

            requisition.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Requisition>().UpdateAsync(requisition);
            await _unitOfWork.SaveChangesAsync();

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
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);

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

            return _mapper.Map<RequisitionResponse>(requisition);
        }
    }
}

