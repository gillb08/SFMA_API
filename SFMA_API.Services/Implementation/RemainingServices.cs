using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Configuration;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Exceptions;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class SchemeAndTimetableService : ISchemeAndTimetableService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SchemeAndTimetableService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SchemeOfWorkResponse>> GetSchemeOfWork(Guid classSectionId, Guid? termId)
        {
            var query = _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable(s => s.ClassSectionId == classSectionId);

            if (termId.HasValue)
            {
                query = query.Where(s => s.AcademicTermId == termId.Value);
            }

            var list = await query.OrderBy(s => s.WeekNumber).ToListAsync();
            return _mapper.Map<IEnumerable<SchemeOfWorkResponse>>(list);
        }

        public async Task<SchemeOfWorkResponse> CreateSchemeOfWork(CreateSchemeOfWorkRequest request)
        {
            var entry = _mapper.Map<SchemeOfWorkEntry>(request);
            entry.Id = Guid.NewGuid();
            entry.VettedByHos = false;
            entry.Status = SchemeStatus.Upcoming;
            entry.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<SchemeOfWorkEntry>().AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SchemeOfWorkResponse>(entry);
        }

        public async Task<SchemeOfWorkResponse> UpdateSchemeOfWork(Guid id, UpdateSchemeOfWorkRequest request)
        {
            var entry = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetByIdAsync(id);
            if (entry == null)
                throw new KeyNotFoundException($"Scheme of work entry with id {id} not found.");

            _mapper.Map(request, entry);
            entry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<SchemeOfWorkEntry>().Update(entry);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SchemeOfWorkResponse>(entry);
        }

        public async Task<bool> VetSchemeOfWork(Guid id, ClaimsPrincipal currentUser)
        {
            var entry = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetByIdAsync(id);
            if (entry == null)
                throw new KeyNotFoundException($"Scheme of work entry with id {id} not found.");

            entry.VettedByHos = true;
            entry.Status = SchemeStatus.InProgress;
            entry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<SchemeOfWorkEntry>().Update(entry);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TimetableSlotResponse>> GetTimetable(Guid classId)
        {
            var slots = await _unitOfWork.GetRepository<TimetableSlot>()
                .GetQueryable(t => t.ClassSectionId == classId)
                .Include(t => t.Teacher)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.PeriodNumber)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TimetableSlotResponse>>(slots);
        }

        public async Task<bool> UpdateTimetable(Guid classId, UpdateTimetableRequest request)
        {
            var repo = _unitOfWork.GetRepository<TimetableSlot>();
            var existingSlots = await repo.GetQueryable(t => t.ClassSectionId == classId).ToListAsync();

            foreach (var slot in existingSlots)
            {
                repo.Delete(slot);
            }

            if (request.Slots != null && request.Slots.Any())
            {
                foreach (var slotReq in request.Slots)
                {
                    var slot = new TimetableSlot
                    {
                        Id = Guid.NewGuid(),
                        ClassSectionId = classId,
                        Subject = slotReq.Subject,
                        TeacherId = slotReq.TeacherId,
                        DayOfWeek = slotReq.DayOfWeek,
                        PeriodNumber = slotReq.PeriodNumber,
                        StartTime = slotReq.StartTime,
                        EndTime = slotReq.EndTime,
                        IsBreak = slotReq.IsBreak,
                        CreatedAt = DateTime.UtcNow
                    };
                    await repo.AddAsync(slot);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }

    public class LessonNoteAndAssignmentService : ILessonNoteAndAssignmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly SchoolSettings _schoolSettings;

        public LessonNoteAndAssignmentService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<SchoolSettings> schoolSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _schoolSettings = schoolSettings.Value;
        }

        public async Task<IEnumerable<LessonNoteResponse>> GetLessonNotes(LessonNoteStatus? status, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<LessonNote>().GetQueryable()
                .Include(l => l.Staff)
                .Include(l => l.ReviewedBy)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            // Supervisor role list comes from configuration — not hardcoded in logic
            var isSupervisor = roles.Any(r => _schoolSettings.SupervisorRoles.Contains(r));

            if (!isSupervisor && roles.Contains("teacher"))
            {
                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();

                if (staff != null)
                {
                    query = query.Where(l => l.StaffId == staff.Id);
                }
            }

            var notes = await query.OrderByDescending(l => l.SubmittedAt).ToListAsync();
            return _mapper.Map<IEnumerable<LessonNoteResponse>>(notes);
        }

        public async Task<LessonNoteResponse> CreateLessonNote(CreateLessonNoteRequest request, ClaimsPrincipal currentUser)
        {
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();

            var count = await _unitOfWork.GetRepository<LessonNote>().GetQueryable().CountAsync();
            var note = _mapper.Map<LessonNote>(request);
            note.Id = Guid.NewGuid();
            note.NoteCode = $"LN-{(count + 1):D4}";
            if (staff != null) note.StaffId = staff.Id;
            note.Status = LessonNoteStatus.Submitted;
            note.SubmittedAt = DateTime.UtcNow;
            note.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<LessonNote>().AddAsync(note);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<LessonNoteResponse>(note);
        }

        public async Task<LessonNoteResponse> ReviewLessonNote(Guid id, ReviewLessonNoteRequest request, ClaimsPrincipal currentUser)
        {
            var note = await _unitOfWork.GetRepository<LessonNote>().GetByIdAsync(id);
            if (note == null)
                throw new KeyNotFoundException($"Lesson note with id {id} not found.");

            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var supervisor = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();

            note.Status = request.Status;
            note.ReviewerComments = request.ReviewerComments;
            if (supervisor != null) note.ReviewedById = supervisor.Id;
            note.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<LessonNote>().Update(note);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<LessonNoteResponse>(note);
        }

        public async Task<IEnumerable<AssignmentResponse>> GetAssignments(Guid classSectionId, ClaimsPrincipal currentUser)
        {
            var assignments = await _unitOfWork.GetRepository<Assignment>().GetQueryable(a => a.ClassSectionId == classSectionId)
                .Include(a => a.Staff)
                .Include(a => a.Submissions)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AssignmentResponse>>(assignments);
        }

        public async Task<AssignmentResponse> CreateAssignment(CreateAssignmentRequest request, ClaimsPrincipal currentUser)
        {
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();

            var count = await _unitOfWork.GetRepository<Assignment>().GetQueryable().CountAsync();
            var assignment = _mapper.Map<Assignment>(request);
            assignment.Id = Guid.NewGuid();
            assignment.AssignmentCode = $"ASG-{(count + 1):D4}";
            if (staff != null) assignment.StaffId = staff.Id;
            assignment.Status = AssignmentStatus.Published;
            assignment.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Assignment>().AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AssignmentResponse>(assignment);
        }

        public async Task<AssignmentResponse> UpdateAssignment(Guid id, UpdateAssignmentRequest request)
        {
            var assignment = await _unitOfWork.GetRepository<Assignment>().GetByIdAsync(id);
            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with id {id} not found.");

            _mapper.Map(request, assignment);
            assignment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<Assignment>().Update(assignment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AssignmentResponse>(assignment);
        }

        public async Task<IEnumerable<AssignmentSubmissionResponse>> GetAssignmentSubmissions(Guid assignmentId, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<AssignmentSubmission>().GetQueryable(s => s.AssignmentId == assignmentId)
                .Include(s => s.Student)
                .AsQueryable();

            var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            if (roles.Contains("student"))
            {
                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var student = await _unitOfWork.GetRepository<Student>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();
                if (student != null)
                {
                    query = query.Where(s => s.StudentId == student.Id);
                }
            }

            var submissions = await query.ToListAsync();
            return _mapper.Map<IEnumerable<AssignmentSubmissionResponse>>(submissions);
        }

        public async Task<AssignmentSubmissionResponse> SubmitAssignment(Guid assignmentId, SubmitAssignmentRequest request, ClaimsPrincipal currentUser)
        {
            var assignment = await _unitOfWork.GetRepository<Assignment>().GetByIdAsync(assignmentId);
            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with id {assignmentId} not found.");

            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var student = await _unitOfWork.GetRepository<Student>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();

            var studentId = student?.Id ?? Guid.Empty;

            // FileName and FileType must be supplied by the caller.
            // No silent defaults — a missing or mismatched type would corrupt submission metadata.
            if (string.IsNullOrWhiteSpace(request.FileName))
                throw new ArgumentException("FileName is required when submitting an assignment.");

            if (string.IsNullOrWhiteSpace(request.FileType))
                throw new ArgumentException("FileType is required when submitting an assignment.");

            var submission = new AssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = studentId,
                FileUrl = request.FileUrl,
                FileName = request.FileName,
                FileType = request.FileType,
                FileSizeBytes = request.FileSizeBytes,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<AssignmentSubmission>().AddAsync(submission);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AssignmentSubmissionResponse>(submission);
        }
    }

    public class InquiryAndRequirementService : IInquiryAndRequirementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InquiryAndRequirementService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponse<InquiryResponse>> GetInquiries(InquiryStatus? status, RequestParameters parameters, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<Inquiry>().GetQueryable()
                .Include(i => i.Parent)
                .Include(i => i.Student)
                .Include(i => i.RoutedTo)
                .Include(i => i.Messages)
                .Include(i => i.TimelineSteps)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (roles.Contains("parent"))
            {
                var parent = await _unitOfWork.GetRepository<Parent>().GetQueryable(p => p.UserId == currentUserId).FirstOrDefaultAsync();
                if (parent != null)
                {
                    query = query.Where(i => i.ParentId == parent.Id);
                }
            }
            else if (roles.Contains("student"))
            {
                var student = await _unitOfWork.GetRepository<Student>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();
                if (student != null)
                {
                    query = query.Where(i => i.StudentId == student.Id);
                }
            }

            var list = await query.ToListAsync();
            var paged = PagedList<Inquiry>.ToPagedList(list, parameters.PageNumber, parameters.PageSize);
            var dtos = _mapper.Map<IEnumerable<InquiryResponse>>(paged);
            return new PagedResponse<InquiryResponse>
            {
                Items = dtos,
                MetaData = paged.MetaData
            };
        }

        public async Task<InquiryResponse?> GetInquiryById(Guid id, ClaimsPrincipal currentUser)
        {
            var inquiry = await _unitOfWork.GetRepository<Inquiry>().GetQueryable(i => i.Id == id)
                .Include(i => i.Parent)
                .Include(i => i.Student)
                .Include(i => i.RoutedTo)
                .Include(i => i.Messages)
                .Include(i => i.TimelineSteps)
                .FirstOrDefaultAsync();

            if (inquiry == null) return null;
            return _mapper.Map<InquiryResponse>(inquiry);
        }

        public async Task<InquiryResponse> CreateInquiry(CreateInquiryRequest request, ClaimsPrincipal currentUser)
        {
            var count = await _unitOfWork.GetRepository<Inquiry>().GetQueryable().CountAsync();
            var inquiry = _mapper.Map<Inquiry>(request);
            inquiry.Id = Guid.NewGuid();
            inquiry.InquiryCode = $"INQ-{(count + 1):D4}";
            inquiry.Status = InquiryStatus.Open;
            inquiry.CreatedAt = DateTime.UtcNow;

            var timeline = new InquiryTimelineStep
            {
                Id = Guid.NewGuid(),
                InquiryId = inquiry.Id,
                StageLabel = "Inquiry Submitted",
                CompletedAt = DateTime.UtcNow,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Inquiry>().AddAsync(inquiry);
            await _unitOfWork.GetRepository<InquiryTimelineStep>().AddAsync(timeline);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InquiryResponse>(inquiry);
        }

        public async Task<InquiryMessageResponse> AddInquiryMessage(Guid id, AddInquiryMessageRequest request, ClaimsPrincipal currentUser)
        {
            var inquiry = await _unitOfWork.GetRepository<Inquiry>().GetByIdAsync(id);
            if (inquiry == null)
                throw new KeyNotFoundException($"Inquiry with id {id} not found.");

            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            var message = new InquiryMessage
            {
                Id = Guid.NewGuid(),
                InquiryId = id,
                SenderId = currentUserId,
                Message = request.Message,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            inquiry.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Inquiry>().Update(inquiry);

            await _unitOfWork.GetRepository<InquiryMessage>().AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InquiryMessageResponse>(message);
        }

        public async Task<InquiryResponse> UpdateInquiryStatus(Guid id, UpdateInquiryStatusRequest request, ClaimsPrincipal currentUser)
        {
            var inquiry = await _unitOfWork.GetRepository<Inquiry>().GetQueryable(i => i.Id == id)
                .Include(i => i.Messages)
                .Include(i => i.TimelineSteps)
                .FirstOrDefaultAsync();

            if (inquiry == null)
                throw new KeyNotFoundException($"Inquiry with id {id} not found.");

            inquiry.Status = request.Status;
            var stepCount = inquiry.TimelineSteps.Count;

            var timeline = new InquiryTimelineStep
            {
                Id = Guid.NewGuid(),
                InquiryId = inquiry.Id,
                StageLabel = $"Status: {request.Status}",
                CompletedAt = DateTime.UtcNow,
                SortOrder = (short)(stepCount + 1),
                CreatedAt = DateTime.UtcNow
            };

            inquiry.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Inquiry>().Update(inquiry);
            await _unitOfWork.GetRepository<InquiryTimelineStep>().AddAsync(timeline);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InquiryResponse>(inquiry);
        }

        public async Task<IEnumerable<SchoolRequirementResponse>> GetSchoolRequirements(Guid classSectionId, Guid? termId, Guid? studentId)
        {
            var query = _unitOfWork.GetRepository<SchoolRequirement>().GetQueryable(r => r.ClassSectionId == classSectionId);

            if (termId.HasValue)
            {
                query = query.Where(r => r.AcademicTermId == termId.Value);
            }

            var requirements = await query.ToListAsync();
            var responses = _mapper.Map<List<SchoolRequirementResponse>>(requirements);

            if (studentId.HasValue)
            {
                var statuses = await _unitOfWork.GetRepository<RequirementStatus>().GetQueryable(s => s.StudentId == studentId.Value).ToListAsync();

                foreach (var resp in responses)
                {
                    var status = statuses.FirstOrDefault(s => s.RequirementId == resp.Id);
                    if (status != null)
                    {
                        resp.Acquired = status.Acquired;
                    }
                }
            }

            return responses;
        }

        public async Task<bool> ToggleRequirementStatus(Guid requirementId, ToggleRequirementStatusRequest request, ClaimsPrincipal currentUser)
        {
            var statusRepo = _unitOfWork.GetRepository<RequirementStatus>();
            var status = await statusRepo.GetQueryable(s => s.RequirementId == requirementId && s.StudentId == request.StudentId).FirstOrDefaultAsync();

            if (status == null)
            {
                status = new RequirementStatus
                {
                    Id = Guid.NewGuid(),
                    RequirementId = requirementId,
                    StudentId = request.StudentId,
                    Acquired = request.Acquired,
                    AcquiredAt = request.Acquired ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow
                };
                await statusRepo.AddAsync(status);
            }
            else
            {
                status.Acquired = request.Acquired;
                status.AcquiredAt = request.Acquired ? DateTime.UtcNow : null;
                status.UpdatedAt = DateTime.UtcNow;
                statusRepo.Update(status);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<string> ExportRequirementsPdf(Guid classSectionId)
        {
            // Previously this returned a hardcoded fake Base64 blob regardless of the class.
            // Now it queries real requirements and returns a structured CSV payload.
            // Full PDF generation (via a library such as QuestPDF or iText) should be
            // implemented here when a PDF library is added to the project.
            var requirements = await _unitOfWork.GetRepository<SchoolRequirement>()
                .GetQueryable(r => r.ClassSectionId == classSectionId)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ItemCode,Title,Category,Author,Publisher,IsRequired");
            foreach (var req in requirements)
            {
                sb.AppendLine($"{req.ItemCode},{req.Title},{req.Category},{req.Author},{req.Publisher},{req.IsRequired}");
            }

            // Return as Base64-encoded CSV until a proper PDF library is integrated
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }

    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SchoolSettings _schoolSettings;

        public DashboardService(IUnitOfWork unitOfWork, IOptions<SchoolSettings> schoolSettings)
        {
            _unitOfWork = unitOfWork;
            _schoolSettings = schoolSettings.Value;
        }

        public async Task<ExecutiveDashboardResponse> GetExecutiveDashboard()
        {
            var totalEnrollment = (int)await _unitOfWork.GetRepository<Student>().CountAsync();
            var staffCount = (int)await _unitOfWork.GetRepository<Staff>().CountAsync();
            var totalRevenue = await _unitOfWork.GetRepository<FeeTransaction>().GetQueryable(f => f.Status == FeeTransactionStatus.Verified).SumAsync(f => (decimal?)f.AmountPaid) ?? 0m;
            var requisitionPipelineCount = (int)await _unitOfWork.GetRepository<Requisition>().GetQueryable(r => r.Status == RequisitionStatus.Submitted || r.Status == RequisitionStatus.InReview).CountAsync();

            return new ExecutiveDashboardResponse
            {
                TotalEnrollment = totalEnrollment,
                TotalRevenue = totalRevenue,
                RequisitionPipelineCount = requisitionPipelineCount,
                StaffCount = staffCount
            };
        }

        public async Task<AcademicSnapshotResponse> GetAcademicSnapshot()
        {
            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>()
                .GetQueryable(t => t.IsActive)
                .FirstOrDefaultAsync();

            // No hardcoded fallback term name — return null/empty if no active term is configured
            if (activeTerm == null)
            {
                throw new KeyNotFoundException(
                    "No active academic term is configured. " +
                    "Please set an active term before viewing the academic snapshot.");
            }

            var termName = $"Term {activeTerm.TermNumber} {activeTerm.SessionName}";
            var totalStudents = (int)await _unitOfWork.GetRepository<Student>().CountAsync();

            // Assessment metrics from real database data
            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetQueryable().ToListAsync();

            // Pass-mark threshold from configuration — not hardcoded as 50
            decimal passThreshold = _schoolSettings.PassMarkThreshold;
            decimal averageScore = scores.Any() ? Math.Round(scores.Average(s => s.TotalScore), 1) : 0m;
            decimal passRate = scores.Any()
                ? Math.Round((decimal)scores.Count(s => s.TotalScore >= passThreshold) / scores.Count * 100m, 1)
                : 0m;
            var distinctEvaluated = scores.Select(s => s.StudentId).Distinct().Count();

            // Scheme vetting metrics from real database data
            var schemes = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable().ToListAsync();
            decimal vettedRate = schemes.Any()
                ? Math.Round((decimal)schemes.Count(s => s.VettedByHos) / schemes.Count * 100m, 1)
                : 0m;

            // Milestone attainment: count students with at least one acquired requirement
            // Previously hardcoded as 96.2m — now computed from real RequirementStatus records
            var acquiredCount = await _unitOfWork.GetRepository<RequirementStatus>()
                .GetQueryable(s => s.Acquired)
                .Select(s => s.StudentId)
                .Distinct()
                .CountAsync();

            decimal milestoneAttainmentRate = totalStudents > 0
                ? Math.Round((decimal)acquiredCount / totalStudents * 100m, 1)
                : 0m;

            // QA threshold from configuration — not hardcoded as 80
            decimal qaThreshold = _schoolSettings.QualityAssuranceVettingThreshold;
            string standing = vettedRate >= qaThreshold
                ? "Approved for Terminal Examinations"
                : (schemes.Any() ? "In Review by Head of Schools" : "Pending Submission");

            return new AcademicSnapshotResponse
            {
                Term = termName,
                EarlyYears = new EarlyYearsSnapshot
                {
                    Focus = "Early Literacy & Motor Skills",
                    MilestoneAttainmentRate = milestoneAttainmentRate,
                    EnrolledPupils = totalStudents
                },
                JuniorBasic = new JuniorBasicSnapshot
                {
                    Focus = "Cognitive & Terminal Assessment Broadsheet",
                    AverageScore = averageScore,
                    PassRate = passRate,
                    EvaluatedScholars = distinctEvaluated
                },
                QualityAssurance = new QualityAssuranceSnapshot
                {
                    SchemesVettedRate = vettedRate,
                    Standing = standing
                }
            };
        }

        public async Task<IEnumerable<BudgetItemProgress>> GetBudgetProgress()
        {
            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>()
                .GetSingleByAsync(t => t.IsActive);

            var requisitions = await _unitOfWork.GetRepository<Requisition>().GetQueryable().ToListAsync();

            // Load all configured department budgets for the active term in one query
            var departmentBudgets = activeTerm != null
                ? await _unitOfWork.GetRepository<DepartmentBudget>()
                    .GetQueryable(b => b.AcademicTermId == activeTerm.Id)
                    .ToListAsync()
                : new List<DepartmentBudget>();

            var groups = requisitions.GroupBy(r => r.Department);
            var progress = new List<BudgetItemProgress>();

            foreach (var g in groups)
            {
                // Budget is read from the DepartmentBudget table — no longer hardcoded as 500,000
                var budgetRecord = departmentBudgets.FirstOrDefault(b => b.Department == g.Key);
                decimal budget = budgetRecord?.AllocatedAmount ?? _schoolSettings.DefaultDepartmentBudget;

                var actual = g.Where(r => r.Status == RequisitionStatus.Approved).Sum(r => r.Amount);
                progress.Add(new BudgetItemProgress
                {
                    Category = g.Key,
                    BudgetedAmount = budget,
                    ActualAmount = actual
                });
            }

            return progress;
        }

        public async Task<FinancialDashboardResponse> GetFinancialDashboard()
        {
            var totalEnrollment = (int)await _unitOfWork.GetRepository<Student>().CountAsync();
            var feeItemsSum = await _unitOfWork.GetRepository<FeeItem>().GetQueryable().SumAsync(f => (decimal?)f.Amount) ?? 0m;
            var totalBilled = feeItemsSum * (totalEnrollment > 0 ? totalEnrollment : 1);

            var totalCollected = await _unitOfWork.GetRepository<FeeTransaction>().GetQueryable(f => f.Status == FeeTransactionStatus.Verified).SumAsync(f => (decimal?)f.AmountPaid) ?? 0m;
            var totalOutstanding = Math.Max(0m, totalBilled - totalCollected);
            var unpostedTellersCount = (int)await _unitOfWork.GetRepository<FeeTransaction>().GetQueryable(f => f.Status == FeeTransactionStatus.Unposted || f.Status == FeeTransactionStatus.PendingReview).CountAsync();

            return new FinancialDashboardResponse
            {
                TotalBilled = totalBilled,
                TotalCollected = totalCollected,
                TotalOutstanding = totalOutstanding,
                UnpostedTellersCount = unpostedTellersCount
            };
        }
    }
}
