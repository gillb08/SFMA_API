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

        public async Task<IEnumerable<SchemeOfWorkResponse>> GetSchemeOfWork(Guid classSectionId, Guid? termId, Guid? subjectId, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, currentUser, classSectionId);

            var query = _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable(s => s.ClassSectionId == classSectionId);

            var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor && (roles.Contains("parent") || roles.Contains("student")))
            {
                query = query.Where(s => s.ApprovalStatus == SchemeApprovalStatus.Approved || s.VettedByHos);
            }

            if (termId.HasValue && termId.Value != Guid.Empty)
            {
                query = query.Where(s => s.AcademicTermId == termId.Value);
            }

            if (subjectId.HasValue && subjectId.Value != Guid.Empty)
            {
                query = query.Where(s => s.SubjectId == subjectId.Value);
            }

            return await query
                .OrderBy(s => s.WeekNumber)
                .Select(s => new SchemeOfWorkResponse
                {
                    Id = s.Id,
                    ClassSectionId = s.ClassSectionId,
                    ClassSectionName = s.ClassSection != null ? s.ClassSection.Name : string.Empty,
                    AcademicTermId = s.AcademicTermId,
                    AcademicTermName = s.AcademicTerm != null ? $"Term {s.AcademicTerm.TermNumber} ({s.AcademicTerm.SessionName})" : string.Empty,
                    SubjectId = s.SubjectId,
                    SubjectName = s.Subject != null ? s.Subject.Name : (s.Topic.StartsWith("[") && s.Topic.Contains("]") ? s.Topic.Substring(1, s.Topic.IndexOf(']') - 1) : "General"),
                    CurriculumTopicId = s.CurriculumTopicId,
                    CurriculumTopicTitle = s.CurriculumTopic != null ? s.CurriculumTopic.Topic : null,
                    WeekNumber = s.WeekNumber,
                    WeekLabel = s.WeekLabel,
                    Topic = s.Topic,
                    LearningObjectives = s.LearningObjectives,
                    MaterialsName = s.MaterialsName,
                    MaterialsUrl = s.MaterialsUrl,
                    Status = s.Status,
                    ApprovalStatus = s.ApprovalStatus != 0 ? s.ApprovalStatus : (s.VettedByHos ? SchemeApprovalStatus.Approved : SchemeApprovalStatus.Draft),
                    ApprovalStatusLabel = (s.ApprovalStatus != 0 ? s.ApprovalStatus : (s.VettedByHos ? SchemeApprovalStatus.Approved : SchemeApprovalStatus.Draft)).ToString(),
                    VettedByHos = s.VettedByHos || s.ApprovalStatus == SchemeApprovalStatus.Approved,
                    CreatedByStaffId = s.CreatedByStaffId,
                    CreatedByStaffName = s.CreatedByStaff != null && s.CreatedByStaff.User != null ? s.CreatedByStaff.User.DisplayName : null,
                    VettedByStaffId = s.VettedByStaffId,
                    VettedByStaffName = s.VettedByStaff != null && s.VettedByStaff.User != null ? s.VettedByStaff.User.DisplayName : null,
                    VettedAt = s.VettedAt,
                    ReviewComment = s.ReviewComment,
                    DeliveredLessonCount = s.LessonNotes.Count(ln => ln.DeliveryStatus == LessonDeliveryStatus.Delivered),
                    TotalLessonCount = s.LessonNotes.Count(),
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<SchemeOfWorkResponse> CreateSchemeOfWork(CreateSchemeOfWorkRequest request, ClaimsPrincipal currentUser)
        {
            if (request.ClassSectionId == Guid.Empty)
                throw new ArgumentException("Class section identifier is required.");
            if (request.AcademicTermId == Guid.Empty)
                throw new ArgumentException("Academic term identifier is required.");

            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassSectionId);
            await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassSectionId, null, request.AcademicTermId);

            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentStaff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == currentUserId, include: q => q.Include(s => s.User));

            CurriculumTopic? topicEntity = null;
            if (request.CurriculumTopicId.HasValue && request.CurriculumTopicId.Value != Guid.Empty)
            {
                topicEntity = await _unitOfWork.GetRepository<CurriculumTopic>().GetByIdAsync(request.CurriculumTopicId.Value);
            }

            var entry = new SchemeOfWorkEntry
            {
                Id = Guid.NewGuid(),
                ClassSectionId = request.ClassSectionId,
                AcademicTermId = request.AcademicTermId,
                SubjectId = request.SubjectId ?? topicEntity?.SubjectId,
                CurriculumTopicId = topicEntity?.Id,
                WeekNumber = request.WeekNumber > 0 ? request.WeekNumber : (short)1,
                WeekLabel = !string.IsNullOrWhiteSpace(request.WeekLabel) ? request.WeekLabel.Trim() : $"Week {request.WeekNumber}",
                Topic = !string.IsNullOrWhiteSpace(request.Topic) ? request.Topic.Trim() : (topicEntity?.Topic ?? "Untitled Topic"),
                LearningObjectives = !string.IsNullOrWhiteSpace(request.LearningObjectives) ? request.LearningObjectives.Trim() : topicEntity?.LearningOutcomes,
                MaterialsName = request.MaterialsName?.Trim(),
                MaterialsUrl = request.MaterialsUrl?.Trim(),
                Status = request.Status ?? SchemeStatus.Upcoming,
                ApprovalStatus = SchemeApprovalStatus.Submitted,
                VettedByHos = false,
                CreatedByStaffId = currentStaff?.Id,
                CreatedAt = DateTime.UtcNow
            };

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "SchemeOfWorkEntry",
                EntityId = entry.Id,
                Action = "Submitted",
                ToStatus = SchemeApprovalStatus.Submitted.ToString(),
                ActorStaffId = currentStaff?.Id,
                ActorName = currentStaff?.User?.DisplayName ?? currentUser.Identity?.Name ?? "Teacher",
                ActorRole = currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "teacher",
                Comments = $"Submitted scheme of work for {entry.WeekLabel}: {entry.Topic}",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<SchemeOfWorkEntry>().AddAsync(entry);
            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            return (await GetSchemeOfWork(entry.ClassSectionId, entry.AcademicTermId, null, currentUser)).First(s => s.Id == entry.Id);
        }

        public async Task<SchemeOfWorkResponse> UpdateSchemeOfWork(Guid id, UpdateSchemeOfWorkRequest request, ClaimsPrincipal currentUser)
        {
            var entry = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetByIdAsync(id);
            if (entry == null)
                throw new KeyNotFoundException($"Scheme of work entry with id {id} not found.");

            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, entry.ClassSectionId);

            var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head");

            if (!isSupervisor && entry.ApprovalStatus == SchemeApprovalStatus.Approved)
            {
                throw new InvalidOperationException("Cannot modify an already approved scheme of work. An academic supervisor must request revisions first.");
            }

            if (request.CurriculumTopicId.HasValue && request.CurriculumTopicId.Value != Guid.Empty)
            {
                var topicEntity = await _unitOfWork.GetRepository<CurriculumTopic>().GetByIdAsync(request.CurriculumTopicId.Value);
                if (topicEntity != null)
                {
                    entry.CurriculumTopicId = topicEntity.Id;
                    if (!request.SubjectId.HasValue) entry.SubjectId = topicEntity.SubjectId;
                    if (string.IsNullOrWhiteSpace(request.Topic)) entry.Topic = topicEntity.Topic;
                    if (string.IsNullOrWhiteSpace(request.LearningObjectives)) entry.LearningObjectives = topicEntity.LearningOutcomes;
                }
            }

            if (request.SubjectId.HasValue && request.SubjectId.Value != Guid.Empty) entry.SubjectId = request.SubjectId.Value;
            if (request.WeekNumber.HasValue) entry.WeekNumber = request.WeekNumber.Value;
            if (!string.IsNullOrWhiteSpace(request.WeekLabel)) entry.WeekLabel = request.WeekLabel.Trim();
            if (!string.IsNullOrWhiteSpace(request.Topic)) entry.Topic = request.Topic.Trim();
            if (request.LearningObjectives != null) entry.LearningObjectives = request.LearningObjectives.Trim();
            if (request.MaterialsName != null) entry.MaterialsName = request.MaterialsName.Trim();
            if (request.MaterialsUrl != null) entry.MaterialsUrl = request.MaterialsUrl.Trim();
            if (request.Status.HasValue) entry.Status = request.Status.Value;
            entry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<SchemeOfWorkEntry>().Update(entry);
            await _unitOfWork.SaveChangesAsync();

            return (await GetSchemeOfWork(entry.ClassSectionId, entry.AcademicTermId, null, currentUser)).First(s => s.Id == entry.Id);
        }

        public async Task<SchemeOfWorkResponse> ReviewSchemeOfWork(Guid id, ReviewSchemeOfWorkRequest request, ClaimsPrincipal currentUser)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "review-scheme-of-work");

            var entry = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetByIdAsync(id);
            if (entry == null)
                throw new KeyNotFoundException($"Scheme of work entry with id {id} not found.");

            if (request.Action == SchemeApprovalStatus.RevisionRequested && string.IsNullOrWhiteSpace(request.ReviewComment))
            {
                throw new ArgumentException("A review comment is mandatory when requesting revisions on a scheme of work.");
            }

            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var supervisorStaff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == currentUserId, include: q => q.Include(s => s.User));

            var prevStatus = entry.ApprovalStatus.ToString();
            entry.ApprovalStatus = request.Action;
            entry.VettedByHos = (request.Action == SchemeApprovalStatus.Approved);
            entry.VettedByStaffId = supervisorStaff?.Id;
            entry.VettedAt = DateTime.UtcNow;
            entry.ReviewComment = request.ReviewComment?.Trim();

            if (request.Action == SchemeApprovalStatus.Approved)
            {
                entry.Status = SchemeStatus.InProgress;
            }
            entry.UpdatedAt = DateTime.UtcNow;

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "SchemeOfWorkEntry",
                EntityId = entry.Id,
                Action = request.Action.ToString(),
                FromStatus = prevStatus,
                ToStatus = request.Action.ToString(),
                ActorStaffId = supervisorStaff?.Id,
                ActorName = supervisorStaff?.User?.DisplayName ?? currentUser.Identity?.Name ?? "Academic Head",
                ActorRole = currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "academic_head",
                Comments = request.ReviewComment,
                Timestamp = DateTime.UtcNow
            };

            _unitOfWork.GetRepository<SchemeOfWorkEntry>().Update(entry);
            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            return (await GetSchemeOfWork(entry.ClassSectionId, entry.AcademicTermId, null, currentUser)).First(s => s.Id == entry.Id);
        }

        public async Task<bool> VetSchemeOfWork(Guid id, ClaimsPrincipal currentUser)
        {
            await ReviewSchemeOfWork(id, new ReviewSchemeOfWorkRequest { Action = SchemeApprovalStatus.Approved }, currentUser);
            return true;
        }

        public async Task<IEnumerable<CurriculumCoverageReportResponse>> GetCoverageReport(Guid? classSectionId, Guid? termId, ClaimsPrincipal currentUser)
        {
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                if (roles.Contains("teacher"))
                {
                    string? currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == currentUserId);
                    var assignedClass = staff != null ? await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id) : null;
                    if (assignedClass == null)
                    {
                        return Enumerable.Empty<CurriculumCoverageReportResponse>();
                    }

                    if (classSectionId.HasValue && classSectionId.Value != Guid.Empty && classSectionId.Value != assignedClass.Id)
                    {
                        throw new UnauthorizedAccessException($"Access denied: You are assigned solely to {assignedClass.Name} and cannot view reports for another class.");
                    }
                    classSectionId = assignedClass.Id;
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Only academic supervisors and assigned teachers can view curriculum coverage reports.");
                }
            }

            var subjectsQuery = _unitOfWork.GetRepository<Subject>().GetQueryable();
            var topicsQuery = _unitOfWork.GetRepository<CurriculumTopic>().GetQueryable(t => t.Active);
            var schemeQuery = _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable();
            var notesQuery = _unitOfWork.GetRepository<LessonNote>().GetQueryable();

            if (termId.HasValue && termId.Value != Guid.Empty)
            {
                topicsQuery = topicsQuery.Where(t => t.AcademicTermId == termId.Value);
                schemeQuery = schemeQuery.Where(s => s.AcademicTermId == termId.Value);
                notesQuery = notesQuery.Where(n => n.AcademicTermId == termId.Value);
            }

            if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
            {
                schemeQuery = schemeQuery.Where(s => s.ClassSectionId == classSectionId.Value);
                notesQuery = notesQuery.Where(n => n.ClassSectionId == classSectionId.Value);
            }

            var classes = await _unitOfWork.GetRepository<ClassSection>().GetQueryable(c => !classSectionId.HasValue || c.Id == classSectionId.Value).ToListAsync();
            var subjects = await subjectsQuery.ToListAsync();
            var topics = await topicsQuery.ToListAsync();
            var schemes = await schemeQuery.ToListAsync();
            var notes = await notesQuery.ToListAsync();

            var report = new List<CurriculumCoverageReportResponse>();

            foreach (var cls in classes)
            {
                foreach (var subj in subjects)
                {
                    var subjTopics = topics.Where(t => t.SubjectId == subj.Id && (t.ClassSectionId == null || t.ClassSectionId == cls.Id)).ToList();
                    var subjSchemes = schemes.Where(s => s.ClassSectionId == cls.Id && (s.SubjectId == subj.Id || s.Topic.Contains(subj.Name))).ToList();
                    var subjNotes = notes.Where(n => n.ClassSectionId == cls.Id && (n.SubjectId == subj.Id || n.Subject.Equals(subj.Name, StringComparison.OrdinalIgnoreCase))).ToList();

                    int planned = subjSchemes.Count;
                    int approved = subjSchemes.Count(s => s.ApprovalStatus == SchemeApprovalStatus.Approved || s.VettedByHos);
                    int delivered = subjNotes.Count(n => n.DeliveryStatus == LessonDeliveryStatus.Delivered);
                    int pendingReviews = subjSchemes.Count(s => s.ApprovalStatus == SchemeApprovalStatus.Submitted) + subjNotes.Count(n => n.Status == LessonNoteStatus.Submitted || n.Status == LessonNoteStatus.UnderReview);
                    int totalTopics = Math.Max(subjTopics.Count, planned);

                    decimal coverage = totalTopics > 0 ? Math.Round((decimal)delivered / totalTopics * 100m, 1) : 0m;
                    int behindSchedule = subjSchemes.Count(s => s.ApprovalStatus == SchemeApprovalStatus.Approved && !subjNotes.Any(n => n.SchemeOfWorkEntryId == s.Id && n.DeliveryStatus == LessonDeliveryStatus.Delivered));

                    if (totalTopics > 0 || planned > 0 || delivered > 0)
                    {
                        report.Add(new CurriculumCoverageReportResponse
                        {
                            ClassSectionId = cls.Id,
                            ClassSectionName = cls.Name,
                            SubjectId = subj.Id,
                            SubjectName = subj.Name,
                            TotalCurriculumTopics = totalTopics,
                            PlannedSchemeWeeks = planned,
                            ApprovedSchemeWeeks = approved,
                            DeliveredLessons = delivered,
                            CoveragePercentage = coverage,
                            PendingReviews = pendingReviews,
                            TopicsBehindScheduleCount = behindSchedule
                        });
                    }
                }
            }

            return report.OrderBy(r => r.ClassSectionName).ThenBy(r => r.SubjectName);
        }

        public async Task<IEnumerable<CurriculumAlertResponse>> GetCurriculumAlerts(Guid? classSectionId, ClaimsPrincipal currentUser)
        {
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                if (roles.Contains("teacher"))
                {
                    string? currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == currentUserId);
                    var assignedClass = staff != null ? await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id) : null;
                    if (assignedClass == null)
                    {
                        return Enumerable.Empty<CurriculumAlertResponse>();
                    }

                    if (classSectionId.HasValue && classSectionId.Value != Guid.Empty && classSectionId.Value != assignedClass.Id)
                    {
                        throw new UnauthorizedAccessException($"Access denied: You are assigned solely to {assignedClass.Name} and cannot view alerts for another class.");
                    }
                    classSectionId = assignedClass.Id;
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Only academic supervisors and assigned teachers can view curriculum alerts.");
                }
            }

            var alerts = new List<CurriculumAlertResponse>();
            var schemesQuery = _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable();
            var notesQuery = _unitOfWork.GetRepository<LessonNote>().GetQueryable();

            if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
            {
                schemesQuery = schemesQuery.Where(s => s.ClassSectionId == classSectionId.Value);
                notesQuery = notesQuery.Where(n => n.ClassSectionId == classSectionId.Value);
            }

            var unvettedSchemes = await schemesQuery.Where(s => s.ApprovalStatus == SchemeApprovalStatus.Submitted)
                .Select(s => new { s.Id, s.WeekLabel, s.Topic, ClassName = s.ClassSection != null ? s.ClassSection.Name : "Class" })
                .Take(10)
                .ToListAsync();

            foreach (var item in unvettedSchemes)
            {
                alerts.Add(new CurriculumAlertResponse
                {
                    Type = "SchemeAwaitingReview",
                    Severity = "Warning",
                    Message = $"Scheme week {item.WeekLabel} ({item.Topic}) for {item.ClassName} is awaiting supervisor vetting.",
                    TargetId = item.Id,
                    TargetTitle = item.Topic
                });
            }

            var pendingNotes = await notesQuery.Where(n => n.Status == LessonNoteStatus.Submitted || n.Status == LessonNoteStatus.UnderReview)
                .Select(n => new { n.Id, n.NoteCode, n.Topic, ClassName = n.ClassSection != null ? n.ClassSection.Name : "Class" })
                .Take(10)
                .ToListAsync();

            foreach (var item in pendingNotes)
            {
                alerts.Add(new CurriculumAlertResponse
                {
                    Type = "LessonNoteAwaitingReview",
                    Severity = "Warning",
                    Message = $"Lesson note {item.NoteCode} ({item.Topic}) for {item.ClassName} is pending supervisor approval.",
                    TargetId = item.Id,
                    TargetTitle = item.Topic
                });
            }

            return alerts;
        }

        public async Task<IEnumerable<TimetableSlotResponse>> GetTimetable(Guid classId, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, currentUser, classId);

            var slots = await _unitOfWork.GetRepository<TimetableSlot>()
                .GetQueryable(t => t.ClassSectionId == classId)
                .Include(t => t.Teacher).ThenInclude(s => s!.User)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.PeriodNumber)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TimetableSlotResponse>>(slots);
        }

        public async Task<bool> UpdateTimetable(Guid classId, UpdateTimetableRequest request, ClaimsPrincipal currentUser)
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required to modify timetable.");
            }

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                if (roles.Contains("teacher"))
                {
                    await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, classId);
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Only academic supervisors or assigned teachers can modify class timetables.");
                }
            }

            var classSection = await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, classId);
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
                    Guid? resolvedTeacherId = slotReq.IsBreak
                        ? null
                        : (slotReq.TeacherId.HasValue && slotReq.TeacherId.Value != Guid.Empty
                            ? slotReq.TeacherId
                            : classSection.HomeroomTeacherId);

                    var slot = new TimetableSlot
                    {
                        Id = Guid.NewGuid(),
                        ClassSectionId = classId,
                        Subject = slotReq.Subject,
                        TeacherId = resolvedTeacherId,
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
        private readonly ISchoolNotificationService _notificationService;

        public LessonNoteAndAssignmentService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<SchoolSettings> schoolSettings, ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _schoolSettings = schoolSettings.Value;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<LessonNoteResponse>> GetLessonNotes(LessonNoteStatus? status, Guid? classSectionId, Guid? schemeOfWorkEntryId, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<LessonNote>().GetQueryable();

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
            {
                query = query.Where(l => l.ClassSectionId == classSectionId.Value);
            }

            if (schemeOfWorkEntryId.HasValue && schemeOfWorkEntryId.Value != Guid.Empty)
            {
                query = query.Where(l => l.SchemeOfWorkEntryId == schemeOfWorkEntryId.Value);
            }

            var roles = currentUser.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            var isSupervisor = roles.Any(r => _schoolSettings.SupervisorRoles.Contains(r))
                || roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head");

            if (!isSupervisor)
            {
                string? currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (roles.Contains("teacher"))
                {
                    var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();

                    if (staff != null)
                    {
                        var assignedClass = await _unitOfWork.GetRepository<ClassSection>().GetSingleByAsync(c => c.HomeroomTeacherId == staff.Id);
                        var assignedClassId = assignedClass?.Id;

                        if (classSectionId.HasValue && classSectionId.Value != Guid.Empty && assignedClassId.HasValue && classSectionId.Value != assignedClassId.Value)
                        {
                            throw new UnauthorizedAccessException($"Access denied: You are assigned solely to {assignedClass?.Name} and cannot view lesson notes for another class.");
                        }

                        query = query.Where(l => l.StaffId == staff.Id || (assignedClassId.HasValue && l.ClassSectionId == assignedClassId.Value));
                    }
                    else
                    {
                        return Enumerable.Empty<LessonNoteResponse>();
                    }
                }
                else if (roles.Contains("student"))
                {
                    var student = await _unitOfWork.GetRepository<Student>().GetSingleByAsync(s => s.UserId == currentUserId);
                    if (student == null)
                    {
                        return Enumerable.Empty<LessonNoteResponse>();
                    }

                    if (classSectionId.HasValue && classSectionId.Value != Guid.Empty && classSectionId.Value != student.ClassSectionId)
                    {
                        throw new UnauthorizedAccessException("Access denied: Students may only view lesson notes for their assigned class.");
                    }

                    query = query.Where(l => l.ClassSectionId == student.ClassSectionId && l.Status == LessonNoteStatus.Approved);
                }
                else if (roles.Contains("parent"))
                {
                    var parent = await _unitOfWork.GetRepository<Parent>().GetSingleByAsync(
                        p => p.UserId == currentUserId,
                        include: q => q.Include(p => p.StudentParents).ThenInclude(sp => sp.Student));

                    if (parent == null || !parent.StudentParents.Any(sp => sp.Student != null))
                    {
                        return Enumerable.Empty<LessonNoteResponse>();
                    }

                    var allowedClassIds = parent.StudentParents
                        .Where(sp => sp.Student != null)
                        .Select(sp => sp.Student!.ClassSectionId)
                        .Distinct()
                        .ToList();

                    if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
                    {
                        if (!allowedClassIds.Contains(classSectionId.Value))
                        {
                            throw new UnauthorizedAccessException("Access denied: Parents may only view lesson notes for classes containing their linked wards.");
                        }
                        query = query.Where(l => l.ClassSectionId == classSectionId.Value && l.Status == LessonNoteStatus.Approved);
                    }
                    else
                    {
                        query = query.Where(l => allowedClassIds.Contains(l.ClassSectionId) && l.Status == LessonNoteStatus.Approved);
                    }
                }
                else
                {
                    return Enumerable.Empty<LessonNoteResponse>();
                }
            }

            return await query
                .OrderByDescending(l => l.SubmittedAt)
                .Select(l => new LessonNoteResponse
                {
                    Id = l.Id,
                    NoteCode = l.NoteCode,
                    StaffId = l.StaffId,
                    StaffName = l.Staff != null && l.Staff.User != null ? l.Staff.User.DisplayName : string.Empty,
                    SubjectId = l.SubjectId,
                    Subject = l.SubjectRef != null ? l.SubjectRef.Name : l.Subject,
                    Topic = l.Topic,
                    ClassSectionId = l.ClassSectionId,
                    ClassSectionName = l.ClassSection != null ? l.ClassSection.Name : string.Empty,
                    AcademicTermId = l.AcademicTermId,
                    AcademicTermName = l.AcademicTerm != null ? $"Term {l.AcademicTerm.TermNumber} ({l.AcademicTerm.SessionName})" : string.Empty,
                    SchemeOfWorkEntryId = l.SchemeOfWorkEntryId,
                    WeekNumber = l.SchemeOfWorkEntry != null ? (short?)l.SchemeOfWorkEntry.WeekNumber : null,
                    LessonDate = l.LessonDate,
                    PeriodNumber = l.PeriodNumber,
                    Objectives = l.Objectives,
                    FileUrl = l.FileUrl,
                    Status = l.Status,
                    StatusLabel = l.Status.ToString(),
                    DeliveryStatus = l.DeliveryStatus,
                    DeliveryStatusLabel = l.DeliveryStatus.ToString(),
                    DeliveredAt = l.DeliveredAt,
                    DeliveryRemark = l.DeliveryRemark,
                    ReviewedById = l.ReviewedById,
                    ReviewedByName = l.ReviewedBy != null && l.ReviewedBy.User != null ? l.ReviewedBy.User.DisplayName : null,
                    ReviewerComments = l.ReviewerComments,
                    SubmittedAt = l.SubmittedAt,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<LessonNoteResponse> CreateLessonNote(CreateLessonNoteRequest request, ClaimsPrincipal currentUser)
        {
            if (request.SchemeOfWorkEntryId == Guid.Empty)
            {
                throw new ArgumentException("A lesson note must be linked to an approved scheme of work entry.");
            }

            var scheme = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable(s => s.Id == request.SchemeOfWorkEntryId)
                .Include(s => s.Subject)
                .FirstOrDefaultAsync();

            if (scheme == null)
            {
                throw new KeyNotFoundException($"Scheme of work entry with id '{request.SchemeOfWorkEntryId}' not found.");
            }

            if (scheme.ApprovalStatus != SchemeApprovalStatus.Approved && !scheme.VettedByHos)
            {
                throw new InvalidOperationException("Lesson notes can only be created against an approved scheme of work entry.");
            }

            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, scheme.ClassSectionId);

            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).Include(s => s.User).FirstOrDefaultAsync();

            if (staff == null)
            {
                throw new UnauthorizedAccessException("Only active staff members can submit lesson notes.");
            }

            var count = await _unitOfWork.GetRepository<LessonNote>().GetQueryable().CountAsync();
            var resolvedFileUrl = !string.IsNullOrWhiteSpace(request.FileUrl) ? request.FileUrl.Trim() : (!string.IsNullOrWhiteSpace(request.AttachmentUrl) ? request.AttachmentUrl.Trim() : null);
            var resolvedObjectives = !string.IsNullOrWhiteSpace(request.Objectives) ? request.Objectives.Trim() : (!string.IsNullOrWhiteSpace(request.ContentSummary) ? request.ContentSummary.Trim() : scheme.LearningObjectives);

            var note = new LessonNote
            {
                Id = Guid.NewGuid(),
                NoteCode = $"LN-{(count + 1):D4}",
                StaffId = staff.Id,
                ClassSectionId = scheme.ClassSectionId,
                AcademicTermId = scheme.AcademicTermId,
                SubjectId = scheme.SubjectId ?? request.SubjectId,
                SchemeOfWorkEntryId = scheme.Id,
                Subject = !string.IsNullOrWhiteSpace(request.Subject) ? request.Subject.Trim() : (scheme.Subject?.Name ?? "General"),
                Topic = !string.IsNullOrWhiteSpace(request.Topic) ? request.Topic.Trim() : scheme.Topic,
                Objectives = resolvedObjectives,
                FileUrl = resolvedFileUrl,
                LessonDate = request.LessonDate ?? DateTime.UtcNow.Date,
                PeriodNumber = request.PeriodNumber,
                Status = LessonNoteStatus.Submitted,
                DeliveryStatus = LessonDeliveryStatus.Planned,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "LessonNote",
                EntityId = note.Id,
                Action = "Submitted",
                ToStatus = LessonNoteStatus.Submitted.ToString(),
                ActorStaffId = staff.Id,
                ActorName = staff.User?.DisplayName ?? currentUser.Identity?.Name ?? "Teacher",
                ActorRole = currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "teacher",
                Comments = $"Submitted lesson note '{note.NoteCode}' for topic: {note.Topic}",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<LessonNote>().AddAsync(note);
            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            return (await GetLessonNotes(null, null, null, currentUser)).First(n => n.Id == note.Id);
        }

        public async Task<LessonNoteResponse> ReviewLessonNote(Guid id, ReviewLessonNoteRequest request, ClaimsPrincipal currentUser)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "review-lesson-note");

            var note = await _unitOfWork.GetRepository<LessonNote>().GetByIdAsync(id);
            if (note == null)
                throw new KeyNotFoundException($"Lesson note with id {id} not found.");

            if (request.Status == LessonNoteStatus.RevisionRequested && string.IsNullOrWhiteSpace(request.ReviewerComments))
            {
                throw new ArgumentException("Reviewer comments are mandatory when requesting revision on a lesson note.");
            }

            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var supervisor = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).Include(s => s.User).FirstOrDefaultAsync();

            var prevStatus = note.Status.ToString();
            note.Status = request.Status;
            note.ReviewerComments = request.ReviewerComments?.Trim();
            if (supervisor != null) note.ReviewedById = supervisor.Id;
            note.UpdatedAt = DateTime.UtcNow;

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "LessonNote",
                EntityId = note.Id,
                Action = request.Status.ToString(),
                FromStatus = prevStatus,
                ToStatus = request.Status.ToString(),
                ActorStaffId = supervisor?.Id,
                ActorName = supervisor?.User?.DisplayName ?? currentUser.Identity?.Name ?? "Academic Head",
                ActorRole = currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "academic_head",
                Comments = request.ReviewerComments,
                Timestamp = DateTime.UtcNow
            };

            _unitOfWork.GetRepository<LessonNote>().Update(note);
            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            // Notify submitting teacher
            var teacher = await _unitOfWork.GetRepository<Staff>()
                .GetSingleByAsync(s => s.Id == note.StaffId, include: q => q.Include(s => s.User));

            if (teacher?.User?.Email != null)
            {
                string reviewStatusLabel = request.Status == LessonNoteStatus.Approved
                    ? "Approved" : "Revision Requested";

                _ = Task.Run(() => _notificationService.SendLessonNoteReviewFeedbackAsync(
                    teacher.User.DisplayName,
                    teacher.User.Email,
                    note.NoteCode,
                    note.Subject,
                    note.Topic,
                    reviewStatusLabel,
                    request.ReviewerComments));
            }

            return (await GetLessonNotes(null, null, null, currentUser)).First(n => n.Id == note.Id);
        }

        public async Task<LessonNoteResponse> MarkLessonDelivery(Guid id, MarkLessonDeliveryRequest request, ClaimsPrincipal currentUser)
        {
            var note = await _unitOfWork.GetRepository<LessonNote>().GetByIdAsync(id);
            if (note == null)
                throw new KeyNotFoundException($"Lesson note with id '{id}' not found.");

            if (note.Status != LessonNoteStatus.Approved)
            {
                throw new InvalidOperationException("Only approved lesson notes can be marked for classroom delivery.");
            }

            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, note.ClassSectionId);

            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == currentUserId, include: q => q.Include(s => s.User));

            var prevDelivery = note.DeliveryStatus.ToString();
            note.DeliveryStatus = request.DeliveryStatus;
            note.DeliveredAt = request.DeliveredAt ?? (request.DeliveryStatus == LessonDeliveryStatus.Delivered ? DateTime.UtcNow : null);
            note.DeliveryRemark = request.DeliveryRemark?.Trim();
            note.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<LessonNote>().Update(note);

            // Progress tracking: update scheme progress from delivered lessons
            if (note.SchemeOfWorkEntryId.HasValue)
            {
                var scheme = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetByIdAsync(note.SchemeOfWorkEntryId.Value);
                if (scheme != null)
                {
                    var allNotes = await _unitOfWork.GetRepository<LessonNote>().GetQueryable(l => l.SchemeOfWorkEntryId == scheme.Id).ToListAsync();
                    bool allDelivered = allNotes.All(l => l.Id == note.Id
                        ? (request.DeliveryStatus == LessonDeliveryStatus.Delivered)
                        : (l.DeliveryStatus == LessonDeliveryStatus.Delivered));

                    if (allDelivered && allNotes.Any())
                    {
                        scheme.Status = SchemeStatus.Completed;
                    }
                    else if (request.DeliveryStatus == LessonDeliveryStatus.Delivered || allNotes.Any(l => l.DeliveryStatus == LessonDeliveryStatus.Delivered))
                    {
                        scheme.Status = SchemeStatus.InProgress;
                    }
                    scheme.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<SchemeOfWorkEntry>().Update(scheme);
                }
            }

            var auditLog = new AcademicWorkflowAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "LessonNote",
                EntityId = note.Id,
                Action = "DeliveryRecorded",
                FromStatus = prevDelivery,
                ToStatus = request.DeliveryStatus.ToString(),
                ActorStaffId = staff?.Id,
                ActorName = staff?.User?.DisplayName ?? currentUser.Identity?.Name ?? "Teacher",
                ActorRole = currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "teacher",
                Comments = request.DeliveryRemark ?? $"Lesson marked {request.DeliveryStatus}",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<AcademicWorkflowAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            return (await GetLessonNotes(null, null, null, currentUser)).First(n => n.Id == note.Id);
        }

        public async Task<IEnumerable<AssignmentResponse>> GetAssignments(Guid classSectionId, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, currentUser, classSectionId);

            var assignments = await _unitOfWork.GetRepository<Assignment>().GetQueryable(a => a.ClassSectionId == classSectionId)
                .Include(a => a.Staff).ThenInclude(s => s.User)
                .Include(a => a.Submissions)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AssignmentResponse>>(assignments);
        }

        public async Task<AssignmentResponse> CreateAssignment(CreateAssignmentRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassSectionId);
            await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassSectionId);

            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.UserId == currentUserId).Include(s => s.User).FirstOrDefaultAsync();

            var count = await _unitOfWork.GetRepository<Assignment>().GetQueryable().CountAsync();
            var assignment = _mapper.Map<Assignment>(request);
            assignment.Id = Guid.NewGuid();
            assignment.AssignmentCode = $"ASG-{(count + 1):D4}";
            if (staff != null) assignment.StaffId = staff.Id;
            assignment.Status = AssignmentStatus.Published;
            assignment.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Assignment>().AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<AssignmentResponse>(assignment);
            if (staff?.User != null) response.StaffName = staff.User.DisplayName;
            return response;
        }

        public async Task<AssignmentResponse> UpdateAssignment(Guid id, UpdateAssignmentRequest request, ClaimsPrincipal currentUser)
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required to update assignments.");
            }

            var assignment = await _unitOfWork.GetRepository<Assignment>().GetByIdAsync(id);
            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with id {id} not found.");

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                if (roles.Contains("teacher"))
                {
                    await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, assignment.ClassSectionId);
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Only academic supervisors or assigned teachers can modify assignments.");
                }
            }

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
                var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

            Guid studentId = Guid.Empty;
            if (request.StudentId != Guid.Empty)
            {
                var targetStudent = await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, request.StudentId);
                studentId = targetStudent.Id;
            }
            else
            {
                var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var student = await _unitOfWork.GetRepository<Student>().GetQueryable(s => s.UserId == currentUserId).FirstOrDefaultAsync();
                if (student != null)
                {
                    studentId = student.Id;
                }
            }

            if (studentId == Guid.Empty)
            {
                throw new InvalidOperationException("Could not identify a valid student record for assignment submission.");
            }

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

        public async Task<AssignmentSubmissionResponse> GradeAssignmentSubmission(Guid submissionId, GradeAssignmentSubmissionRequest request, ClaimsPrincipal currentUser)
        {
            var submission = await _unitOfWork.GetRepository<AssignmentSubmission>()
                .GetQueryable(s => s.Id == submissionId)
                .Include(s => s.Student)
                .FirstOrDefaultAsync();

            if (submission == null)
                throw new KeyNotFoundException($"Assignment submission with id {submissionId} not found.");

            submission.Grade = request.Grade;
            await _unitOfWork.GetRepository<AssignmentSubmission>().UpdateAsync(submission);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AssignmentSubmissionResponse>(submission);
        }
    }

    public class InquiryAndRequirementService : IInquiryAndRequirementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISchoolNotificationService _notificationService;

        public InquiryAndRequirementService(IUnitOfWork unitOfWork, IMapper mapper, ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
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
            var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

            // Ensure subject line is set
            if (string.IsNullOrWhiteSpace(inquiry.SubjectLine))
            {
                inquiry.SubjectLine = !string.IsNullOrWhiteSpace(request.Subject)
                    ? request.Subject
                    : $"Admission Inquiry: {request.ChildName ?? "New Scholar"} for {request.ClassApplying ?? "Admission"}";
            }

            // Bind prospective fields
            inquiry.ParentName = request.ParentName ?? inquiry.ParentName;
            inquiry.ParentPhone = request.ParentPhone ?? inquiry.ParentPhone;
            inquiry.ParentEmail = request.ParentEmail ?? inquiry.ParentEmail;
            inquiry.ChildName = request.ChildName ?? inquiry.ChildName;
            inquiry.ClassApplying = request.ClassApplying ?? inquiry.ClassApplying;
            inquiry.Message = request.Message ?? inquiry.Message;

            // Resolve ParentId if authenticated parent and not explicitly supplied
            if (!inquiry.ParentId.HasValue || inquiry.ParentId.Value == Guid.Empty)
            {
                if (currentUser?.Identity?.IsAuthenticated == true)
                {
                    var currentUserId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var parent = await _unitOfWork.GetRepository<Parent>().GetQueryable(p => p.UserId == currentUserId).FirstOrDefaultAsync();
                    inquiry.ParentId = parent?.Id;
                }
                else
                {
                    inquiry.ParentId = null;
                }
            }

            // Resolve StudentId
            if (inquiry.StudentId.HasValue && inquiry.StudentId.Value == Guid.Empty)
            {
                inquiry.StudentId = null;
            }

            // Resolve RoutedToId (if not supplied, route to admissions staff or first staff member)
            if (!inquiry.RoutedToId.HasValue || inquiry.RoutedToId.Value == Guid.Empty)
            {
                var admissionsStaff = await _unitOfWork.GetRepository<Staff>()
                    .GetSingleByAsync(s => s.Department.Contains("Admin") || s.Title.Contains("Admissions") || s.Department.Contains("Academic"));
                inquiry.RoutedToId = admissionsStaff?.Id;
            }

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

            // If an initial message was supplied, add it as the first InquiryMessage
            if (!string.IsNullOrWhiteSpace(request.Message))
            {
                var initialMsg = new InquiryMessage
                {
                    Id = Guid.NewGuid(),
                    InquiryId = inquiry.Id,
                    SenderId = currentUser?.Identity?.IsAuthenticated == true ? (currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value) : null,
                    SenderName = !string.IsNullOrWhiteSpace(inquiry.ParentName) ? inquiry.ParentName : "Prospective Parent",
                    SenderRole = inquiry.ParentId.HasValue ? "Parent" : "Prospect",
                    Message = request.Message,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.GetRepository<InquiryMessage>().AddAsync(initialMsg);
            }

            await _unitOfWork.SaveChangesAsync();

            // Notify the routed staff member about the new inquiry if assigned
            if (inquiry.RoutedToId.HasValue)
            {
                var routedStaff = await _unitOfWork.GetRepository<Staff>()
                    .GetSingleByAsync(s => s.Id == inquiry.RoutedToId.Value,
                        include: q => q.Include(s => s.User));

                if (routedStaff?.User?.Email != null)
                {
                    string parentForNotif = inquiry.ParentName ?? "Guardian";
                    string studentForNotif = inquiry.ChildName ?? "Scholar";

                    if (inquiry.ParentId.HasValue)
                    {
                        var p = await _unitOfWork.GetRepository<Parent>().GetSingleByAsync(x => x.Id == inquiry.ParentId.Value);
                        if (p != null) parentForNotif = p.FullName;
                    }

                    if (inquiry.StudentId.HasValue)
                    {
                        var s = await _unitOfWork.GetRepository<Student>().GetSingleByAsync(x => x.Id == inquiry.StudentId.Value);
                        if (s != null) studentForNotif = s.FullName;
                    }

                    _ = Task.Run(() => _notificationService.SendInquiryCreatedAlertAsync(
                        routedStaff.User.DisplayName,
                        routedStaff.User.Email,
                        inquiry.InquiryCode,
                        inquiry.Category,
                        inquiry.SubjectLine,
                        parentForNotif,
                        studentForNotif));
                }
            }

            return _mapper.Map<InquiryResponse>(inquiry);
        }

        public async Task<InquiryMessageResponse> AddInquiryMessage(Guid id, AddInquiryMessageRequest request, ClaimsPrincipal currentUser)
        {
            var inquiry = await _unitOfWork.GetRepository<Inquiry>().GetByIdAsync(id);
            if (inquiry == null)
                throw new KeyNotFoundException($"Inquiry with id {id} not found.");

            var currentUserId = currentUser?.FindFirst("Id")?.Value ?? currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderRoles = currentUser?.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            string senderDisplayName = currentUser?.FindFirst(ClaimTypes.Name)?.Value
                ?? currentUser?.FindFirst("DisplayName")?.Value
                ?? (senderRoles.Contains("parent") ? "Parent" : "Staff");

            var message = new InquiryMessage
            {
                Id = Guid.NewGuid(),
                InquiryId = id,
                SenderId = !string.IsNullOrEmpty(currentUserId) ? currentUserId : null,
                SenderName = senderDisplayName,
                SenderRole = senderRoles.FirstOrDefault() ?? "Staff",
                Message = request.Message,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            inquiry.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Inquiry>().Update(inquiry);

            await _unitOfWork.GetRepository<InquiryMessage>().AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            // Notify the other party about the new message
            var inquiryForNotif = await _unitOfWork.GetRepository<Inquiry>()
                .GetSingleByAsync(i => i.Id == id,
                    include: q => q.Include(i => i.Parent!).Include(i => i.RoutedTo!).ThenInclude(s => s!.User));

            string preview = request.Message.Length > 120 ? request.Message[..120] + "…" : request.Message;

            if (senderRoles.Contains("parent") && inquiryForNotif?.RoutedTo?.User != null)
            {
                _ = Task.Run(() => _notificationService.SendInquiryReplyAlertAsync(
                    inquiryForNotif.RoutedTo.User.DisplayName,
                    inquiryForNotif.RoutedTo.User.Email ?? "",
                    inquiryForNotif.InquiryCode,
                    inquiryForNotif.SubjectLine,
                    senderDisplayName,
                    preview));
            }
            else if (inquiryForNotif?.Parent != null)
            {
                _ = Task.Run(() => _notificationService.SendInquiryReplyAlertAsync(
                    inquiryForNotif.Parent.FullName,
                    inquiryForNotif.Parent.Email,
                    inquiryForNotif.InquiryCode,
                    inquiryForNotif.SubjectLine,
                    senderDisplayName,
                    preview));
            }
            else if (!string.IsNullOrEmpty(inquiryForNotif?.ParentEmail))
            {
                _ = Task.Run(() => _notificationService.SendInquiryReplyAlertAsync(
                    inquiryForNotif.ParentName ?? "Guardian",
                    inquiryForNotif.ParentEmail,
                    inquiryForNotif.InquiryCode,
                    inquiryForNotif.SubjectLine,
                    senderDisplayName,
                    preview));
            }

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

            // Notify the parent about the inquiry status change
            var inquiryWithParent = await _unitOfWork.GetRepository<Inquiry>()
                .GetSingleByAsync(i => i.Id == id,
                    include: q => q.Include(i => i.Parent!));

            if (inquiryWithParent?.Parent != null)
            {
                _ = Task.Run(() => _notificationService.SendInquiryStatusChangedAlertAsync(
                    inquiryWithParent.Parent.FullName,
                    inquiryWithParent.Parent.Email,
                    inquiryWithParent.InquiryCode,
                    inquiryWithParent.SubjectLine,
                    request.Status.ToString()));
            }
            else if (!string.IsNullOrEmpty(inquiryWithParent?.ParentEmail))
            {
                _ = Task.Run(() => _notificationService.SendInquiryStatusChangedAlertAsync(
                    inquiryWithParent.ParentName ?? "Guardian",
                    inquiryWithParent.ParentEmail,
                    inquiryWithParent.InquiryCode,
                    inquiryWithParent.SubjectLine,
                    request.Status.ToString()));
            }

            return _mapper.Map<InquiryResponse>(inquiry);
        }

        public async Task<IEnumerable<SchoolRequirementResponse>> GetSchoolRequirements(Guid classSectionId, Guid? termId, Guid? studentId, ClaimsPrincipal? currentUser = null)
        {
            if (studentId.HasValue && studentId.Value != Guid.Empty && currentUser != null)
            {
                await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, studentId.Value);
            }
            else if (currentUser != null)
            {
                await ClassSectionHelper.ValidateClassAccessForPortalUserAsync(_unitOfWork, currentUser, classSectionId);
            }

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

        public async Task<SchoolRequirementResponse> UpsertSchoolRequirement(UpsertSchoolRequirementRequest request, ClaimsPrincipal currentUser)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (currentUser == null)
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required to modify school requirements.");

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                if (roles.Contains("teacher"))
                {
                    await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassSectionId);
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Only academic administrators or assigned teachers can manage school requirements.");
                }
            }

            var classSection = await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassSectionId, request.ClassName, request.AcademicTermId);
            Guid termId = request.AcademicTermId.HasValue && request.AcademicTermId.Value != Guid.Empty
                ? request.AcademicTermId.Value
                : classSection.AcademicTermId;

            var repo = _unitOfWork.GetRepository<SchoolRequirement>();
            SchoolRequirement? item = null;

            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                item = await repo.GetByIdAsync(request.Id.Value);
                if (item == null)
                    throw new KeyNotFoundException("School requirement not found.");
            }

            if (item == null)
            {
                var count = await repo.CountAsync();
                item = new SchoolRequirement
                {
                    Id = Guid.NewGuid(),
                    ItemCode = $"REQ-ITEM-{(count + 1):D4}",
                    ClassSectionId = classSection.Id,
                    AcademicTermId = termId,
                    Category = request.Category.Trim(),
                    Title = request.Title.Trim(),
                    Author = request.Author?.Trim(),
                    Publisher = request.Publisher?.Trim(),
                    IsRequired = request.IsRequired,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };
                await repo.AddAsync(item);
            }
            else
            {
                item.ClassSectionId = classSection.Id;
                item.AcademicTermId = termId;
                item.Category = request.Category.Trim();
                item.Title = request.Title.Trim();
                item.Author = request.Author?.Trim();
                item.Publisher = request.Publisher?.Trim();
                item.IsRequired = request.IsRequired;
                item.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(item);
            }

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SchoolRequirementResponse>(item);
        }

        public async Task<bool> DeleteSchoolRequirement(Guid id, ClaimsPrincipal currentUser)
        {
            if (currentUser == null)
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required to delete school requirements.");

            var repo = _unitOfWork.GetRepository<SchoolRequirement>();
            var item = await repo.GetByIdAsync(id);
            if (item == null)
                throw new KeyNotFoundException("School requirement not found.");

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSupervisor = roles.Contains("super_admin") || roles.Contains("academic_admin") || roles.Contains("academic_head") || roles.Contains("admin");

            if (!isSupervisor)
            {
                if (roles.Contains("teacher"))
                {
                    await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, item.ClassSectionId);
                }
                else
                {
                    throw new UnauthorizedAccessException("Access denied: Only academic administrators or assigned teachers can delete school requirements.");
                }
            }

            var statusRepo = _unitOfWork.GetRepository<RequirementStatus>();
            await statusRepo.DeleteRangeAsync(s => s.RequirementId == id);
            repo.Delete(item);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleRequirementStatus(Guid requirementId, ToggleRequirementStatusRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, request.StudentId);

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
            var requirements = await _unitOfWork.GetRepository<SchoolRequirement>()
                .GetQueryable(r => r.ClassSectionId == classSectionId)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ItemCode,Title,Category,Author,Publisher,IsRequired");
            foreach (var req in requirements)
            {
                sb.AppendLine($"{req.ItemCode},{req.Title},{req.Category},{req.Author},{req.Publisher},{req.IsRequired}");
            }

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

            if (activeTerm == null)
            {
                throw new KeyNotFoundException(
                    "No active academic term is configured. " +
                    "Please set an active term before viewing the academic snapshot.");
            }

            var termName = $"Term {activeTerm.TermNumber} {activeTerm.SessionName}";
            var totalStudents = (int)await _unitOfWork.GetRepository<Student>().CountAsync();

            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetQueryable().ToListAsync();

            decimal passThreshold = _schoolSettings.PassMarkThreshold;
            decimal averageScore = scores.Any() ? Math.Round(scores.Average(s => s.TotalScore), 1) : 0m;
            decimal passRate = scores.Any()
                ? Math.Round((decimal)scores.Count(s => s.TotalScore >= passThreshold) / scores.Count * 100m, 1)
                : 0m;
            var distinctEvaluated = scores.Select(s => s.StudentId).Distinct().Count();

            var schemes = await _unitOfWork.GetRepository<SchemeOfWorkEntry>().GetQueryable().ToListAsync();
            decimal vettedRate = schemes.Any()
                ? Math.Round((decimal)schemes.Count(s => s.VettedByHos) / schemes.Count * 100m, 1)
                : 0m;

            var acquiredCount = await _unitOfWork.GetRepository<RequirementStatus>()
                .GetQueryable(s => s.Acquired)
                .Select(s => s.StudentId)
                .Distinct()
                .CountAsync();

            decimal milestoneAttainmentRate = totalStudents > 0
                ? Math.Round((decimal)acquiredCount / totalStudents * 100m, 1)
                : 0m;

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
                .GetSingleByAsync(t => t.IsActive)
                ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

            var requisitions = await _unitOfWork.GetRepository<Requisition>().GetQueryable().ToListAsync();

            var departmentBudgets = activeTerm != null
                ? await _unitOfWork.GetRepository<DepartmentBudget>()
                    .GetQueryable(b => b.AcademicTermId == activeTerm.Id)
                    .ToListAsync()
                : new List<DepartmentBudget>();

            var allDepartments = departmentBudgets.Select(b => b.Department)
                .Union(requisitions.Select(r => r.Department), StringComparer.OrdinalIgnoreCase)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToList();

            var progress = new List<BudgetItemProgress>();

            foreach (var dept in allDepartments)
            {
                var budgetRecord = departmentBudgets.FirstOrDefault(b => b.Department.Equals(dept, StringComparison.OrdinalIgnoreCase));
                decimal budget = budgetRecord?.AllocatedAmount ?? _schoolSettings.DefaultDepartmentBudget;

                var actual = requisitions
                    .Where(r => r.Department.Equals(dept, StringComparison.OrdinalIgnoreCase) && (r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Disbursed))
                    .Sum(r => r.Amount);

                progress.Add(new BudgetItemProgress
                {
                    Category = budgetRecord?.Department ?? dept,
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

    public class SubjectService : ISubjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SchoolSettings _schoolSettings;

        public SubjectService(IUnitOfWork unitOfWork, IOptions<SchoolSettings> schoolSettings)
        {
            _unitOfWork = unitOfWork;
            _schoolSettings = schoolSettings.Value;
        }

        public async Task<IEnumerable<SubjectResponse>> GetAllSubjects(
            string? division = null,
            string? department = null,
            Guid? classSectionId = null,
            Guid? academicTermId = null,
            bool activeOnly = true)
        {
            var query = _unitOfWork.GetRepository<Subject>().GetQueryable();

            if (activeOnly)
            {
                query = query.Where(s => s.Active);
            }

            if (classSectionId.HasValue && classSectionId.Value != Guid.Empty)
            {
                query = query.Where(s => s.ClassSectionId == null || s.ClassSectionId == classSectionId.Value);
            }

            if (academicTermId.HasValue && academicTermId.Value != Guid.Empty)
            {
                query = query.Where(s => s.AcademicTermId == null || s.AcademicTermId == academicTermId.Value);
            }

            if (!string.IsNullOrWhiteSpace(division))
            {
                string normDiv = division.Trim().ToLower();
                query = query.Where(s =>
                    (s.Division != null && s.Division.ToLower() == normDiv)
                    || (s.Category != null && s.Category.ToLower() == normDiv));
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                string normDept = department.Trim().ToLower();
                query = query.Where(s =>
                    (s.Department != null && s.Department.ToLower() == normDept)
                    || (s.Category != null && s.Category.ToLower() == normDept));
            }

            return await query
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .Select(s => new SubjectResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Category = s.Category ?? s.Division ?? s.Department,
                    Division = s.Division ?? s.Category,
                    Department = s.Department,
                    ClassSectionId = s.ClassSectionId,
                    ClassSectionName = s.ClassSection != null ? s.ClassSection.Name : null,
                    AcademicTermId = s.AcademicTermId,
                    SortOrder = s.SortOrder,
                    Active = s.Active
                })
                .ToListAsync();
        }

        public async Task<SubjectResponse?> GetSubjectById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("A valid subject ID is required.");
            }

            return await _unitOfWork.GetRepository<Subject>().GetQueryable(s => s.Id == id)
                .Select(s => new SubjectResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Category = s.Category ?? s.Division ?? s.Department,
                    Division = s.Division ?? s.Category,
                    Department = s.Department,
                    ClassSectionId = s.ClassSectionId,
                    ClassSectionName = s.ClassSection != null ? s.ClassSection.Name : null,
                    AcademicTermId = s.AcademicTermId,
                    SortOrder = s.SortOrder,
                    Active = s.Active
                })
                .FirstOrDefaultAsync();
        }

        public async Task<SubjectResponse> CreateSubject(CreateSubjectRequest request, ClaimsPrincipal? currentUser = null)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "create-subject");

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Subject name is required.");
            }

            string normalizedName = request.Name.Trim();
            Guid? classId = (request.ClassSectionId.HasValue && request.ClassSectionId.Value != Guid.Empty) ? request.ClassSectionId.Value : null;
            Guid? termId = (request.AcademicTermId.HasValue && request.AcademicTermId.Value != Guid.Empty) ? request.AcademicTermId.Value : null;

            var repo = _unitOfWork.GetRepository<Subject>();
            var existing = await repo.GetSingleByAsync(s =>
                s.Name.ToLower() == normalizedName.ToLower() &&
                s.ClassSectionId == classId);

            if (existing != null)
            {
                existing.Active = request.Active ?? true;
                if (!string.IsNullOrWhiteSpace(request.Code)) existing.Code = request.Code.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(request.Category)) existing.Category = request.Category.Trim();
                if (!string.IsNullOrWhiteSpace(request.Division)) existing.Division = request.Division.Trim();
                if (!string.IsNullOrWhiteSpace(request.Department)) existing.Department = request.Department.Trim();
                if (termId.HasValue) existing.AcademicTermId = termId;
                if (request.SortOrder.HasValue) existing.SortOrder = request.SortOrder.Value;
                existing.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();
                return (await GetSubjectById(existing.Id))!;
            }

            long totalCount = await repo.CountAsync();
            var subject = new Subject
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                Code = !string.IsNullOrWhiteSpace(request.Code) ? request.Code.Trim().ToUpperInvariant() : null,
                Category = !string.IsNullOrWhiteSpace(request.Category)
                    ? request.Category.Trim()
                    : (!string.IsNullOrWhiteSpace(request.Division) ? request.Division.Trim() : request.Department?.Trim()),
                Division = !string.IsNullOrWhiteSpace(request.Division)
                    ? request.Division.Trim()
                    : request.Category?.Trim(),
                Department = !string.IsNullOrWhiteSpace(request.Department) ? request.Department.Trim() : null,
                ClassSectionId = classId,
                AcademicTermId = termId,
                SortOrder = request.SortOrder ?? (short)(totalCount + 1),
                Active = request.Active ?? true,
                CreatedAt = DateTime.UtcNow
            };

            await repo.AddAsync(subject);
            await _unitOfWork.SaveChangesAsync();

            return (await GetSubjectById(subject.Id))!;
        }

        public async Task<SubjectResponse> UpdateSubject(Guid id, UpdateSubjectRequest request, ClaimsPrincipal? currentUser = null)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "update-subject");

            if (id == Guid.Empty)
            {
                throw new ArgumentException("A valid subject ID is required.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Subject name is required.");
            }

            var repo = _unitOfWork.GetRepository<Subject>();
            var existing = await repo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new KeyNotFoundException("Curriculum subject not found.");
            }

            existing.Name = request.Name.Trim();
            existing.Code = !string.IsNullOrWhiteSpace(request.Code) ? request.Code.Trim().ToUpperInvariant() : existing.Code;
            if (request.Category != null) existing.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
            if (request.Division != null) existing.Division = string.IsNullOrWhiteSpace(request.Division) ? null : request.Division.Trim();
            if (request.Department != null) existing.Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();
            existing.ClassSectionId = (request.ClassSectionId.HasValue && request.ClassSectionId.Value != Guid.Empty) ? request.ClassSectionId.Value : null;
            if (request.AcademicTermId.HasValue)
            {
                existing.AcademicTermId = request.AcademicTermId.Value != Guid.Empty ? request.AcademicTermId.Value : null;
            }
            if (request.SortOrder.HasValue) existing.SortOrder = request.SortOrder.Value;
            if (request.Active.HasValue) existing.Active = request.Active.Value;
            existing.UpdatedAt = DateTime.UtcNow;

            await repo.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            return (await GetSubjectById(existing.Id))!;
        }

        public async Task<bool> DeleteSubject(Guid id, ClaimsPrincipal? currentUser = null)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(currentUser, "delete-subject");

            if (id == Guid.Empty)
            {
                throw new ArgumentException("A valid subject ID is required.");
            }

            var repo = _unitOfWork.GetRepository<Subject>();
            var existing = await repo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new KeyNotFoundException("Curriculum subject not found.");
            }

            repo.Delete(existing);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<SchoolConfigResponse> GetSchoolConfig(ClaimsPrincipal? currentUser = null)
        {
            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>().GetQueryable(t => t.IsActive && t.Active)
                .Select(t => new TermResponse
                {
                    Id = t.Id,
                    SessionName = t.SessionName,
                    TermNumber = t.TermNumber,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    IsActive = t.IsActive,
                    Cat1Deadline = t.Cat1Deadline,
                    Cat2Deadline = t.Cat2Deadline,
                    ExamDeadline = t.ExamDeadline
                })
                .FirstOrDefaultAsync();

            var roles = currentUser?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value.ToLowerInvariant())
                .ToList() ?? new List<string>();

            bool isStaffOrManagement = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("academic_head")
                || roles.Contains("financial_admin")
                || roles.Contains("financial_head")
                || roles.Contains("teacher");

            List<InquiryRecipientResponse> recipients;
            if (isStaffOrManagement)
            {
                recipients = await _unitOfWork.GetRepository<Staff>().GetQueryable(s => s.Active && s.User.Active)
                    .OrderBy(s => s.Department)
                    .Select(s => new InquiryRecipientResponse
                    {
                        StaffId = s.Id,
                        DisplayName = s.User != null && !string.IsNullOrEmpty(s.User.DisplayName) ? s.User.DisplayName : s.Title,
                        RoleLabel = !string.IsNullOrEmpty(s.Title)
                            ? $"{s.Title} ({(s.User != null ? s.User.DisplayName : s.Title)})"
                            : (s.User != null ? s.User.DisplayName : s.Title),
                        Department = s.Department
                    })
                    .ToListAsync();
            }
            else
            {
                recipients = _schoolSettings.InquiryRoutingRoles
                    .Select(role => new InquiryRecipientResponse
                    {
                        StaffId = null,
                        DisplayName = role,
                        RoleLabel = role,
                        Department = "School Administration"
                    })
                    .ToList();
            }

            if (!recipients.Any())
            {
                recipients = _schoolSettings.InquiryRoutingRoles
                    .Select(role => new InquiryRecipientResponse
                    {
                        StaffId = null,
                        DisplayName = role,
                        RoleLabel = role,
                        Department = "School Administration"
                    })
                    .ToList();
            }

            var dynamicDivisions = await _unitOfWork.GetRepository<ClassSection>().GetQueryable(c => c.Active && !string.IsNullOrEmpty(c.Division))
                .Select(c => c.Division)
                .Distinct()
                .ToListAsync();

            var mergedDivisions = _schoolSettings.Divisions
                .Union(dynamicDivisions, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new SchoolConfigResponse
            {
                SchoolHours = _schoolSettings.SchoolHours,
                AssemblyTime = _schoolSettings.AssemblyTime,
                Departments = _schoolSettings.Departments,
                Divisions = mergedDivisions,
                SupportedFileFormats = _schoolSettings.SupportedFileFormats,
                InquiryRoutingRoles = _schoolSettings.InquiryRoutingRoles,
                InquiryRecipients = recipients,
                ActiveTerm = activeTerm
            };
        }
    }
}
