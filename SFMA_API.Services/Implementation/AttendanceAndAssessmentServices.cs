using AutoMapper;
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
    // ─────────────────────────────────────────────────────────────────────────
    // String constants for audit/workflow labels — no longer scattered as
    // inline string literals throughout the service methods.
    // ─────────────────────────────────────────────────────────────────────────
    internal static class AttendanceConstants
    {
        public const string AuditActionUnlock         = "Register Unlocked for Roll Correction";
        public const string InquiryCategoryAbsence    = "Absence Notice";
        public const string TimelineLabelAbsenceFiled = "Absence Notice Filed";
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISchoolNotificationService _notificationService;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<AttendanceRecordResponse>> GetAttendance(Guid classId, DateTime date, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, classId);

            var records = await _unitOfWork.GetRepository<AttendanceRecord>().GetByAsync(
                a => a.ClassSectionId == classId && a.Date.Date == date.Date,
                include: q => q.Include(a => a.Student).Include(a => a.RecordedBy).ThenInclude(r => r.User));

            var mappedRecords = _mapper.Map<List<AttendanceRecordResponse>>(records);
            var recordedStudentIds = mappedRecords.Select(r => r.StudentId).ToHashSet();

            var enrolledStudents = await _unitOfWork.GetRepository<Student>().GetQueryable(s => s.ClassSectionId == classId)
                .OrderBy(s => s.StudentCode)
                .Select(s => new { s.Id, s.FullName, s.StudentCode })
                .ToListAsync();

            foreach (var student in enrolledStudents)
            {
                if (!recordedStudentIds.Contains(student.Id))
                {
                    mappedRecords.Add(new AttendanceRecordResponse
                    {
                        Id = Guid.Empty,
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        StudentCode = student.StudentCode,
                        ClassSectionId = classId,
                        Date = date.Date,
                        Status = AttendanceStatus.Unmarked,
                        ArrivalTime = null,
                        Notes = null,
                        Locked = false
                    });
                }
            }

            return mappedRecords.OrderBy(r => r.StudentCode).ToList();
        }

        public async Task<IEnumerable<AttendanceRecordResponse>> GetStudentMonthlyAttendance(Guid studentId, int month, int year, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, studentId);

            var records = await _unitOfWork.GetRepository<AttendanceRecord>().GetByAsync(
                a => a.StudentId == studentId && a.Date.Month == month && a.Date.Year == year,
                include: q => q.Include(a => a.Student).Include(a => a.RecordedBy).ThenInclude(r => r.User),
                orderBy: q => q.OrderBy(a => a.Date));

            return _mapper.Map<IEnumerable<AttendanceRecordResponse>>(records);
        }

        public async Task<BatchUpdateAttendanceResponse> BatchUpdateAttendance(BatchUpdateAttendanceRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassId);
            await ClassSectionHelper.EnsureClassSectionAsync(_unitOfWork, request.ClassId);

            var attendanceRepo = _unitOfWork.GetRepository<AttendanceRecord>();
            var auditRepo = _unitOfWork.GetRepository<AttendanceAuditLog>();

            var existingRecords = await attendanceRepo.GetByAsync(a => a.ClassSectionId == request.ClassId && a.Date.Date == request.Date.Date);
            bool isLocked = existingRecords.Any(a => a.Locked);

            if (isLocked)
            {
                // Check if supervisor unlocked register for this date
                bool wasUnlocked = await auditRepo.AnyAsync(a => a.ClassSectionId == request.ClassId && a.Date.Date == request.Date.Date);
                if (!wasUnlocked)
                {
                    throw new RecordLockedException();
                }
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId)
                ?? await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => true); // fallback for admin

            if (staff == null)
            {
                throw new InvalidOperationException("No staff account found to record attendance.");
            }

            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive)
                ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

            if (activeTerm == null)
            {
                throw new InvalidOperationException("No active academic term found to record attendance.");
            }

            int updatedCount = 0;
            foreach (var item in request.Records)
            {
                if (item.Status == AttendanceStatus.Unmarked)
                {
                    continue;
                }

                var record = existingRecords.FirstOrDefault(r => r.StudentId == item.StudentId);
                if (record == null)
                {
                    record = new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        StudentId = item.StudentId,
                        ClassSectionId = request.ClassId,
                        Date = request.Date.Date,
                        Status = item.Status,
                        ArrivalTime = item.ArrivalTime,
                        Notes = item.Notes,
                        RecordedById = staff.Id,
                        Locked = false,
                        AcademicTermId = activeTerm.Id
                    };
                    await attendanceRepo.AddAsync(record);
                }
                else
                {
                    record.Status = item.Status;
                    record.ArrivalTime = item.ArrivalTime;
                    record.Notes = item.Notes;
                    record.UpdatedAt = DateTime.UtcNow;
                    await attendanceRepo.UpdateAsync(record);
                }
                updatedCount++;
            }

            await _unitOfWork.SaveChangesAsync();

            return new BatchUpdateAttendanceResponse
            {
                Updated = updatedCount,
                Synced = true
            };
        }

        public async Task<bool> LockAttendance(LockAttendanceRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassId);
            var attendanceRepo = _unitOfWork.GetRepository<AttendanceRecord>();
            var records = await attendanceRepo.GetByAsync(a => a.ClassSectionId == request.ClassId && a.Date.Date == request.Date.Date);
            foreach (var r in records)
            {
                r.Locked = true;
                r.UpdatedAt = DateTime.UtcNow;
                await attendanceRepo.UpdateAsync(r);
            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<UnlockAttendanceResponse> UnlockAttendance(UnlockAttendanceRequest request, ClaimsPrincipal currentUser)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new ArgumentException("Reason is mandatory for unlocking the attendance register.");
            }

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User))
                ?? await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => true);

            if (staff == null)
            {
                throw new InvalidOperationException("No staff account found to authorize register unlocking.");
            }

            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            // Record the actual role — do not fall back to a hardcoded role name that would corrupt the audit trail
            string supervisorRole = roles.FirstOrDefault() ?? "unknown_role";

            var auditLog = new AttendanceAuditLog
            {
                Id = Guid.NewGuid(),
                ClassSectionId = request.ClassId,
                Date = request.Date.Date,
                UnlockedById = staff.Id,
                RoleAtAction = supervisorRole,
                Reason = request.Reason,
                // Audit action label from constant — not an inline string
                Action = AttendanceConstants.AuditActionUnlock,
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<AttendanceAuditLog>().AddAsync(auditLog);

            // Unlock the records
            var records = await _unitOfWork.GetRepository<AttendanceRecord>().GetByAsync(a => a.ClassSectionId == request.ClassId && a.Date.Date == request.Date.Date);
            foreach (var r in records)
            {
                r.Locked = false;
                r.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.GetRepository<AttendanceRecord>().UpdateAsync(r);
            }

            await _unitOfWork.SaveChangesAsync();

            return new UnlockAttendanceResponse
            {
                Success = true,
                ClassId = request.ClassId,
                Date = request.Date,
                UnlockedBy = staff?.User?.DisplayName ?? "Unknown",
                Timestamp = auditLog.Timestamp
            };
        }

        public async Task<IEnumerable<AttendanceAuditLogResponse>> GetAuditLogs(Guid? classSectionId, DateTime? date)
        {
            var query = _unitOfWork.GetRepository<AttendanceAuditLog>().GetQueryable(include: q => q.Include(a => a.UnlockedBy).ThenInclude(u => u.User));
            if (classSectionId.HasValue) query = query.Where(a => a.ClassSectionId == classSectionId.Value);
            if (date.HasValue) query = query.Where(a => a.Date.Date == date.Value.Date);

            var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();
            return _mapper.Map<IEnumerable<AttendanceAuditLogResponse>>(logs);
        }

        public async Task<string> ExportAttendanceCsv(Guid classId, DateTime startDate, DateTime endDate)
        {
            var records = await _unitOfWork.GetRepository<AttendanceRecord>().GetByAsync(
                a => a.ClassSectionId == classId && a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date,
                include: q => q.Include(a => a.Student),
                orderBy: q => q.OrderBy(a => a.Date));

            var sb = new StringBuilder();
            sb.AppendLine("Student Code,Student Name,Date,Status,Arrival Time,Notes");
            foreach (var r in records)
            {
                sb.AppendLine($"{r.Student.StudentCode},{r.Student.FullName},{r.Date:yyyy-MM-dd},{r.Status},{r.ArrivalTime},{r.Notes}");
            }
            return sb.ToString();
        }

        public async Task<bool> SubmitAbsenceNotice(AbsenceNoticeRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, request.StudentId);

            var student = await _unitOfWork.GetRepository<Student>()
                .GetQueryable(s => s.Id == request.StudentId)
                .Include(s => s.StudentParents).ThenInclude(sp => sp.Parent)
                .Include(s => s.ClassSection)
                .FirstOrDefaultAsync();

            if (student == null)
                return false;

            var currentUserId = currentUser?.FindFirst("Id")?.Value ?? currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parent = student.StudentParents.Select(sp => sp.Parent).FirstOrDefault(p => p.UserId == currentUserId)
                         ?? student.StudentParents.Select(sp => sp.Parent).FirstOrDefault();

            var teacherId = student.ClassSection?.HomeroomTeacherId;
            if (!teacherId.HasValue || teacherId.Value == Guid.Empty)
            {
                var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable().FirstOrDefaultAsync();
                teacherId = staff?.Id;
            }

            var count = await _unitOfWork.GetRepository<Inquiry>().GetQueryable().CountAsync();
            var inquiry = new Inquiry
            {
                Id = Guid.NewGuid(),
                InquiryCode = $"ABS-{(count + 1):D4}",
                ParentId = parent?.Id,
                StudentId = student.Id,
                ParentName = parent?.FullName ?? "Parent / Guardian",
                ChildName = student.FullName,
                // Inquiry category from constant — not an inline string
                Category = AttendanceConstants.InquiryCategoryAbsence,
                SubjectLine = $"Absence Notice for {student.FullName} on {request.Date:dd MMM yyyy}",
                RoutedToId = teacherId.HasValue && teacherId.Value != Guid.Empty ? teacherId.Value : null,
                Status = InquiryStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            var message = new InquiryMessage
            {
                Id = Guid.NewGuid(),
                InquiryId = inquiry.Id,
                SenderId = currentUserId ?? string.Empty,
                Message = string.IsNullOrWhiteSpace(request.Reason) ? $"Scholar {student.FullName} will be absent on {request.Date:dd MMM yyyy}." : request.Reason,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var timeline = new InquiryTimelineStep
            {
                Id = Guid.NewGuid(),
                InquiryId = inquiry.Id,
                // Timeline label from constant — not an inline string
                StageLabel = AttendanceConstants.TimelineLabelAbsenceFiled,
                CompletedAt = DateTime.UtcNow,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Inquiry>().AddAsync(inquiry);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                await _unitOfWork.GetRepository<InquiryMessage>().AddAsync(message);
            }
            await _unitOfWork.GetRepository<InquiryTimelineStep>().AddAsync(timeline);
            await _unitOfWork.SaveChangesAsync();

            // Notify homeroom teacher about the absence notice
            var absenceTeacher = await _unitOfWork.GetRepository<Staff>()
                .GetSingleByAsync(s => s.Id == teacherId,
                    include: q => q.Include(s => s.User));

            if (absenceTeacher?.User?.Email != null)
            {
                _ = Task.Run(() => _notificationService.SendAbsenceNoticeAlertAsync(
                    absenceTeacher.User.DisplayName,
                    absenceTeacher.User.Email,
                    student.FullName,
                    student.StudentCode,
                    student.ClassSection?.Name ?? "Class",
                    request.Date,
                    request.Reason ?? "No reason provided"));
            }

            return true;
        }
    }

    public class AssessmentService : IAssessmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly SchoolSettings _schoolSettings;
        private readonly ISchoolNotificationService _notificationService;

        public AssessmentService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<SchoolSettings> schoolSettings, ISchoolNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _schoolSettings = schoolSettings.Value;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<AssessmentScoreResponse>> GetBroadsheet(Guid classId, string subject, Guid termId, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, classId);

            var existingScores = await _unitOfWork.GetRepository<AssessmentScore>().GetQueryable(
                a => a.Student.ClassSectionId == classId && a.Subject.ToLower() == subject.ToLower() && a.AcademicTermId == termId)
                .Select(a => new AssessmentScoreResponse
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = a.Student != null ? a.Student.FullName : string.Empty,
                    StudentCode = a.Student != null ? a.Student.StudentCode : string.Empty,
                    Subject = a.Subject,
                    Ca1Score = a.Ca1Score,
                    Ca2Score = a.Ca2Score,
                    ProjectScore = a.ProjectScore,
                    ExamScore = a.ExamScore,
                    TotalScore = a.TotalScore,
                    Grade = a.Grade,
                    Remark = a.Remark,
                    Status = a.Status,
                    ApprovedByName = a.ApprovedBy != null && a.ApprovedBy.User != null ? a.ApprovedBy.User.DisplayName : null,
                    AcademicTermId = a.AcademicTermId,
                    AcademicTermName = a.AcademicTerm != null ? $"Term {a.AcademicTerm.TermNumber} ({a.AcademicTerm.SessionName})" : string.Empty,
                    SessionName = a.AcademicTerm != null ? a.AcademicTerm.SessionName : string.Empty,
                    TermNumber = a.AcademicTerm != null ? (int?)a.AcademicTerm.TermNumber : null,
                    PublishedAt = a.PublishedAt,
                    ClassTeacherName = a.Student != null && a.Student.ClassSection != null && a.Student.ClassSection.HomeroomTeacher != null && a.Student.ClassSection.HomeroomTeacher.User != null
                        ? a.Student.ClassSection.HomeroomTeacher.User.DisplayName
                        : null,
                    ClassTeacherRemark = a.ClassTeacherRemark ?? a.Remark,
                    HosName = a.ApprovedBy != null && a.ApprovedBy.User != null ? a.ApprovedBy.User.DisplayName : null,
                    HosRemark = a.HosRemark
                })
                .ToListAsync();

            var recordedStudentIds = existingScores.Select(s => s.StudentId).ToHashSet();

            var enrolledStudents = await _unitOfWork.GetRepository<Student>().GetQueryable(s => s.ClassSectionId == classId && s.Status != StudentStatus.Withdrawn && s.Status != StudentStatus.Graduated)
                .OrderBy(s => s.StudentCode)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.StudentCode,
                    ClassTeacherName = s.ClassSection != null && s.ClassSection.HomeroomTeacher != null && s.ClassSection.HomeroomTeacher.User != null
                        ? s.ClassSection.HomeroomTeacher.User.DisplayName
                        : null,
                    AcademicSessionName = s.ClassSection != null && s.ClassSection.AcademicTerm != null
                        ? s.ClassSection.AcademicTerm.SessionName
                        : string.Empty,
                    TermNumber = s.ClassSection != null && s.ClassSection.AcademicTerm != null
                        ? (int?)s.ClassSection.AcademicTerm.TermNumber
                        : null
                })
                .ToListAsync();

            var result = new List<AssessmentScoreResponse>(existingScores);

            foreach (var student in enrolledStudents)
            {
                if (!recordedStudentIds.Contains(student.Id))
                {
                    result.Add(new AssessmentScoreResponse
                    {
                        Id = Guid.Empty,
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        StudentCode = student.StudentCode,
                        Subject = subject,
                        Ca1Score = 0,
                        Ca2Score = 0,
                        ProjectScore = 0,
                        ExamScore = 0,
                        TotalScore = 0,
                        Grade = "-",
                        Remark = "Ungraded",
                        Status = AssessmentStatus.Draft,
                        AcademicTermId = termId,
                        SessionName = student.AcademicSessionName,
                        TermNumber = student.TermNumber,
                        ClassTeacherName = student.ClassTeacherName
                    });
                }
            }

            return result.OrderBy(s => s.StudentCode).ThenBy(s => s.StudentName);
        }

        public async Task<BatchUpdateAssessmentsResponse> BatchUpdateScores(BatchUpdateAssessmentsRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassId);

            var term = await _unitOfWork.GetRepository<AcademicTerm>().GetByIdAsync(request.TermId);
            if (term == null)
            {
                throw new KeyNotFoundException("Academic term not found");
            }

            var now = DateTime.UtcNow;
            var assessmentRepo = _unitOfWork.GetRepository<AssessmentScore>();
            var existingScores = await assessmentRepo.GetByAsync(
                a => a.Subject.ToLower() == request.Subject.ToLower() && a.AcademicTermId == request.TermId,
                include: q => q.Include(a => a.Student));

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId)
                ?? await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => true);

            if (staff == null)
            {
                throw new InvalidOperationException("No staff account found to submit assessment scores.");
            }

            var computedList = new List<ComputedScoreResponse>();
            int savedCount = 0;

            foreach (var item in request.Scores)
            {
                var record = existingScores.FirstOrDefault(s => s.StudentId == item.StudentId);

                // Deadline Lockdown Enforcement
                if (term.Cat1Deadline.HasValue && now > term.Cat1Deadline.Value && item.Ca1.HasValue && (record == null || record.Ca1Score != item.Ca1))
                {
                    throw new DeadlineElapsedException("CAT1_DEADLINE_ELAPSED", "CAT 1 upload deadline has elapsed.");
                }

                if (term.Cat2Deadline.HasValue && now > term.Cat2Deadline.Value && item.Ca2.HasValue && (record == null || record.Ca2Score != item.Ca2))
                {
                    throw new DeadlineElapsedException("CAT2_DEADLINE_ELAPSED", "CAT 2 upload deadline has elapsed.");
                }

                if (term.ExamDeadline.HasValue && now > term.ExamDeadline.Value && item.Exam.HasValue && (record == null || record.ExamScore != item.Exam))
                {
                    throw new DeadlineElapsedException("EXAM_DEADLINE_ELAPSED", "Exam score upload deadline has elapsed.");
                }

                decimal total = (item.Ca1 ?? 0) + (item.Ca2 ?? 0) + (item.Exam ?? 0);

                // Grade resolved from configuration — thresholds no longer hardcoded in the switch
                string grade = _schoolSettings.ResolveGrade(total);

                if (record == null)
                {
                    record = new AssessmentScore
                    {
                        Id = Guid.NewGuid(),
                        StudentId = item.StudentId,
                        Subject = request.Subject,
                        Ca1Score = item.Ca1,
                        Ca2Score = item.Ca2,
                        ProjectScore = 0,
                        ExamScore = item.Exam,
                        TotalScore = total,
                        Grade = grade,
                        Remark = item.Remark,
                        ClassTeacherRemark = item.Remark,
                        AcademicTermId = request.TermId,
                        SubmittedById = staff.Id,
                        Status = AssessmentStatus.Draft
                    };
                    await assessmentRepo.AddAsync(record);
                }
                else
                {
                    if (item.Ca1.HasValue) record.Ca1Score = item.Ca1;
                    if (item.Ca2.HasValue) record.Ca2Score = item.Ca2;
                    record.ProjectScore = 0;
                    if (item.Exam.HasValue) record.ExamScore = item.Exam;
                    record.TotalScore = total;
                    record.Grade = grade;
                    if (item.Remark != null)
                    {
                        record.Remark = item.Remark;
                        record.ClassTeacherRemark = item.Remark;
                    }
                    record.UpdatedAt = DateTime.UtcNow;
                    await assessmentRepo.UpdateAsync(record);
                }

                savedCount++;
                computedList.Add(new ComputedScoreResponse
                {
                    StudentId = item.StudentId,
                    Total = total,
                    Grade = grade
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return new BatchUpdateAssessmentsResponse
            {
                Saved = savedCount,
                Computed = computedList
            };
        }

        public async Task<bool> SubmitBroadsheet(BroadsheetActionRequest request, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassId);
            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == request.ClassId && a.Subject.ToLower() == request.Subject.ToLower() && a.AcademicTermId == request.TermId);

            foreach (var score in scores)
            {
                score.Status = AssessmentStatus.Submitted;
                if (!string.IsNullOrWhiteSpace(request.Remark))
                {
                    score.ClassTeacherRemark = request.Remark.Trim();
                }
                score.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.GetRepository<AssessmentScore>().UpdateAsync(score);
            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveBroadsheet(BroadsheetActionRequest request, ClaimsPrincipal currentUser)
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Access denied: Authenticated user context is required to approve and publish results.");
            }

            var roles = currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value.ToLowerInvariant())
                .ToList();

            bool isAuthorizedSupervisor = roles.Contains("super_admin")
                || roles.Contains("academic_admin")
                || roles.Contains("academic_head");

            bool hasApprovePermission = currentUser.Claims.Any(c =>
                (c.Type == "Permission" || c.Type == "permission" || c.Type == "claims")
                && (string.Equals(c.Value, "all", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Value, "approve-broadsheet", StringComparison.OrdinalIgnoreCase)));

            if (!isAuthorizedSupervisor && !hasApprovePermission)
            {
                throw new UnauthorizedAccessException("Access denied: Only academic supervisors or head of school are authorized to approve and publish broadsheet results.");
            }

            await ClassSectionHelper.ValidateTeacherClassAccessAsync(_unitOfWork, currentUser, request.ClassId);

            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Access denied: Unable to resolve approver identity.");
            }

            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);
            if (staff == null)
            {
                throw new InvalidOperationException("Approver account does not have a verified staff profile to sign off on broadsheet publication.");
            }

            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == request.ClassId && a.Subject.ToLower() == request.Subject.ToLower() && a.AcademicTermId == request.TermId);

            var publishedTimestamp = DateTime.UtcNow;
            foreach (var score in scores)
            {
                score.Status = AssessmentStatus.Published;
                score.ApprovedById = staff.Id;
                score.PublishedAt = publishedTimestamp;
                if (!string.IsNullOrWhiteSpace(request.Remark))
                {
                    score.HosRemark = request.Remark.Trim();
                }
                score.UpdatedAt = publishedTimestamp;
                await _unitOfWork.GetRepository<AssessmentScore>().UpdateAsync(score);
            }
            await _unitOfWork.SaveChangesAsync();

            // Notify parents that results have been approved and published
            var term = await _unitOfWork.GetRepository<AcademicTerm>().GetByIdAsync(request.TermId);
            string termLabel = term != null ? $"Term {term.TermNumber} {term.SessionName}" : "Current Term";

            var notifiedParentIds = new HashSet<Guid>();
            foreach (var score in scores)
            {
                var studentWithParents = await _unitOfWork.GetRepository<Student>()
                    .GetSingleByAsync(s => s.Id == score.StudentId,
                        include: q => q.Include(s => s.StudentParents).ThenInclude(sp => sp.Parent)
                                       .Include(s => s.ClassSection));

                var parent = studentWithParents?.StudentParents
                    .Where(sp => sp.IsPrimary).Select(sp => sp.Parent).FirstOrDefault()
                    ?? studentWithParents?.StudentParents.Select(sp => sp.Parent).FirstOrDefault();

                if (parent != null && notifiedParentIds.Add(parent.Id))
                {
                    var capturedParent = parent;
                    var capturedStudent = studentWithParents!;
                    _ = Task.Run(() => _notificationService.SendResultsPublishedAlertAsync(
                        capturedParent.FullName,
                        capturedParent.Email,
                        capturedParent.Phone,
                        capturedStudent.FullName,
                        capturedStudent.StudentCode,
                        capturedStudent.ClassSection?.Name ?? "Class",
                        termLabel,
                        request.Subject));
                }
            }
            return true;
        }

        public async Task<IEnumerable<AssessmentScoreResponse>> GetStudentResults(Guid studentId, Guid? termId, ClaimsPrincipal currentUser)
        {
            await ClassSectionHelper.ValidateStudentOwnershipAsync(_unitOfWork, currentUser, studentId);

            var query = _unitOfWork.GetRepository<AssessmentScore>().GetQueryable(a => a.StudentId == studentId);

            var isStaff = currentUser != null && (currentUser.IsInRole("super_admin") || currentUser.IsInRole("academic_admin") || currentUser.IsInRole("academic_head") || currentUser.IsInRole("teacher"));
            if (!isStaff)
            {
                query = query.Where(a => a.Status == AssessmentStatus.Published);
            }

            if (termId.HasValue) query = query.Where(a => a.AcademicTermId == termId.Value);

            return await query
                .Select(a => new AssessmentScoreResponse
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = a.Student != null ? a.Student.FullName : string.Empty,
                    StudentCode = a.Student != null ? a.Student.StudentCode : string.Empty,
                    Subject = a.Subject,
                    Ca1Score = a.Ca1Score,
                    Ca2Score = a.Ca2Score,
                    ProjectScore = a.ProjectScore,
                    ExamScore = a.ExamScore,
                    TotalScore = a.TotalScore,
                    Grade = a.Grade,
                    Remark = a.Remark,
                    Status = a.Status,
                    ApprovedByName = a.ApprovedBy != null && a.ApprovedBy.User != null ? a.ApprovedBy.User.DisplayName : null,
                    AcademicTermId = a.AcademicTermId,
                    AcademicTermName = a.AcademicTerm != null ? $"Term {a.AcademicTerm.TermNumber} ({a.AcademicTerm.SessionName})" : string.Empty,
                    SessionName = a.AcademicTerm != null ? a.AcademicTerm.SessionName : string.Empty,
                    TermNumber = a.AcademicTerm != null ? (int?)a.AcademicTerm.TermNumber : null,
                    PublishedAt = a.PublishedAt,
                    ClassTeacherName = a.Student != null && a.Student.ClassSection != null && a.Student.ClassSection.HomeroomTeacher != null && a.Student.ClassSection.HomeroomTeacher.User != null
                        ? a.Student.ClassSection.HomeroomTeacher.User.DisplayName
                        : null,
                    ClassTeacherRemark = a.ClassTeacherRemark ?? a.Remark,
                    HosName = a.ApprovedBy != null && a.ApprovedBy.User != null ? a.ApprovedBy.User.DisplayName : null,
                    HosRemark = a.HosRemark
                })
                .ToListAsync();
        }

        public async Task<string> ExportResults(Guid classId, string subject, Guid termId, string format)
        {
            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == classId && a.Subject.ToLower() == subject.ToLower() && a.AcademicTermId == termId,
                include: q => q.Include(a => a.Student));

            var sb = new StringBuilder();
            sb.AppendLine("Student Code,Student Name,Subject,CA1,CA2,Exam,Total,Grade,Remark");
            foreach (var s in scores)
            {
                sb.AppendLine($"{s.Student.StudentCode},{s.Student.FullName},{s.Subject},{s.Ca1Score},{s.Ca2Score},{s.ExamScore},{s.TotalScore},{s.Grade},{s.Remark}");
            }
            return sb.ToString();
        }

        public async Task<BatchUpdateAssessmentsResponse> ImportScoresCsv(Guid classId, string subject, Guid termId, string csvContent, ClaimsPrincipal currentUser)
        {
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var items = new List<AssessmentScoreItemRequest>();

            var students = await _unitOfWork.GetRepository<Student>().GetByAsync(s => s.ClassSectionId == classId);

            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    string code = parts[0].Trim();
                    var student = students.FirstOrDefault(s => s.StudentCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (student != null)
                    {
                        decimal.TryParse(parts[2], out var ca1);
                        decimal.TryParse(parts[3], out var ca2);
                        decimal exam = 0;
                        string remark = string.Empty;

                        if (parts.Length >= 7)
                        {
                            // Legacy 7-column CSV: StudentCode, StudentName, CA1, CA2, Project, Exam, Remark
                            decimal.TryParse(parts[5], out exam);
                            remark = parts[6].Trim();
                        }
                        else
                        {
                            // 20-20-60 6-column CSV: StudentCode, StudentName, CA1, CA2, Exam, Remark
                            decimal.TryParse(parts[4], out exam);
                            if (parts.Length > 5) remark = parts[5].Trim();
                        }

                        items.Add(new AssessmentScoreItemRequest
                        {
                            StudentId = student.Id,
                            Ca1 = ca1,
                            Ca2 = ca2,
                            Project = 0,
                            Exam = exam,
                            Remark = remark
                        });
                    }
                }
            }

            return await BatchUpdateScores(new BatchUpdateAssessmentsRequest
            {
                ClassId = classId,
                Subject = subject,
                TermId = termId,
                Scores = items
            }, currentUser);
        }

        public Task<string> GetScoreTemplateCsv()
        {
            // Template headers reflect the 20-20-60 assessment model: CA1 (20), CA2 (20), Exam (60)
            return Task.FromResult("StudentCode,StudentName,CA1,CA2,Exam,Remark\n");
        }
    }
}
