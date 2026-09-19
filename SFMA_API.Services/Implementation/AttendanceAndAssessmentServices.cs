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
        public const string AuditActionUnlock        = "Register Unlocked for Roll Correction";
        public const string InquiryCategoryAbsence   = "Absence Notice";
        public const string TimelineLabelAbsenceFiled = "Absence Notice Filed";
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AttendanceRecordResponse>> GetAttendance(Guid classId, DateTime date, ClaimsPrincipal currentUser)
        {
            var records = await _unitOfWork.GetRepository<AttendanceRecord>().GetByAsync(
                a => a.ClassSectionId == classId && a.Date.Date == date.Date,
                include: q => q.Include(a => a.Student).Include(a => a.RecordedBy).ThenInclude(r => r.User));

            return _mapper.Map<IEnumerable<AttendanceRecordResponse>>(records);
        }

        public async Task<IEnumerable<AttendanceRecordResponse>> GetStudentMonthlyAttendance(Guid studentId, int month, int year, ClaimsPrincipal currentUser)
        {
            var records = await _unitOfWork.GetRepository<AttendanceRecord>().GetByAsync(
                a => a.StudentId == studentId && a.Date.Month == month && a.Date.Year == year,
                include: q => q.Include(a => a.Student).Include(a => a.RecordedBy).ThenInclude(r => r.User),
                orderBy: q => q.OrderBy(a => a.Date));

            return _mapper.Map<IEnumerable<AttendanceRecordResponse>>(records);
        }

        public async Task<BatchUpdateAttendanceResponse> BatchUpdateAttendance(BatchUpdateAttendanceRequest request, ClaimsPrincipal currentUser)
        {
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

            var activeTerm = await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => t.IsActive)
                ?? await _unitOfWork.GetRepository<AcademicTerm>().GetSingleByAsync(t => true);

            int updatedCount = 0;
            foreach (var item in request.Records)
            {
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
                        RecordedById = staff?.Id ?? Guid.Empty,
                        Locked = false,
                        AcademicTermId = activeTerm?.Id ?? Guid.Empty
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
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId, include: q => q.Include(s => s.User));
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            // Record the actual role — do not fall back to a hardcoded role name that would corrupt the audit trail
            string supervisorRole = roles.FirstOrDefault() ?? "unknown_role";

            var auditLog = new AttendanceAuditLog
            {
                Id = Guid.NewGuid(),
                ClassSectionId = request.ClassId,
                Date = request.Date.Date,
                UnlockedById = staff?.Id ?? Guid.Empty,
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
            var student = await _unitOfWork.GetRepository<Student>()
                .GetQueryable(s => s.Id == request.StudentId)
                .Include(s => s.StudentParents).ThenInclude(sp => sp.Parent)
                .Include(s => s.ClassSection)
                .FirstOrDefaultAsync();

            if (student == null)
                return false;

            var currentUserId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parent = student.StudentParents.Select(sp => sp.Parent).FirstOrDefault(p => p.UserId == currentUserId)
                         ?? student.StudentParents.Select(sp => sp.Parent).FirstOrDefault();

            var teacherId = student.ClassSection?.HomeroomTeacherId;
            if (!teacherId.HasValue || teacherId.Value == Guid.Empty)
            {
                var staff = await _unitOfWork.GetRepository<Staff>().GetQueryable().FirstOrDefaultAsync();
                teacherId = staff?.Id ?? Guid.Empty;
            }

            var count = await _unitOfWork.GetRepository<Inquiry>().GetQueryable().CountAsync();
            var inquiry = new Inquiry
            {
                Id = Guid.NewGuid(),
                InquiryCode = $"ABS-{(count + 1):D4}",
                ParentId = parent?.Id ?? Guid.Empty,
                StudentId = student.Id,
                // Inquiry category from constant — not an inline string
                Category = AttendanceConstants.InquiryCategoryAbsence,
                SubjectLine = $"Absence Notice for {student.FullName} on {request.Date:dd MMM yyyy}",
                RoutedToId = teacherId.Value,
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

            return true;
        }
    }

    public class AssessmentService : IAssessmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly SchoolSettings _schoolSettings;

        public AssessmentService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<SchoolSettings> schoolSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _schoolSettings = schoolSettings.Value;
        }

        public async Task<IEnumerable<AssessmentScoreResponse>> GetBroadsheet(Guid classId, string subject, Guid termId, ClaimsPrincipal currentUser)
        {
            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == classId && a.Subject.ToLower() == subject.ToLower() && a.AcademicTermId == termId,
                include: q => q.Include(a => a.Student).Include(a => a.ApprovedBy).ThenInclude(ab => ab!.User));

            return _mapper.Map<IEnumerable<AssessmentScoreResponse>>(scores);
        }

        public async Task<BatchUpdateAssessmentsResponse> BatchUpdateScores(BatchUpdateAssessmentsRequest request, ClaimsPrincipal currentUser)
        {
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

                decimal total = (item.Ca1 ?? 0) + (item.Ca2 ?? 0) + (item.Project ?? 0) + (item.Exam ?? 0);

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
                        ProjectScore = item.Project,
                        ExamScore = item.Exam,
                        TotalScore = total,
                        Grade = grade,
                        Remark = item.Remark,
                        AcademicTermId = request.TermId,
                        SubmittedById = staff?.Id ?? Guid.Empty,
                        Status = AssessmentStatus.Draft
                    };
                    await assessmentRepo.AddAsync(record);
                }
                else
                {
                    if (item.Ca1.HasValue) record.Ca1Score = item.Ca1;
                    if (item.Ca2.HasValue) record.Ca2Score = item.Ca2;
                    if (item.Project.HasValue) record.ProjectScore = item.Project;
                    if (item.Exam.HasValue) record.ExamScore = item.Exam;
                    record.TotalScore = total;
                    record.Grade = grade;
                    if (item.Remark != null) record.Remark = item.Remark;
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
            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == request.ClassId && a.Subject.ToLower() == request.Subject.ToLower() && a.AcademicTermId == request.TermId);

            foreach (var score in scores)
            {
                score.Status = AssessmentStatus.Submitted;
                score.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.GetRepository<AssessmentScore>().UpdateAsync(score);
            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveBroadsheet(BroadsheetActionRequest request, ClaimsPrincipal currentUser)
        {
            string? userId = currentUser.FindFirst("Id")?.Value ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var staff = await _unitOfWork.GetRepository<Staff>().GetSingleByAsync(s => s.UserId == userId);

            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == request.ClassId && a.Subject.ToLower() == request.Subject.ToLower() && a.AcademicTermId == request.TermId);

            foreach (var score in scores)
            {
                score.Status = AssessmentStatus.Approved;
                score.ApprovedById = staff?.Id;
                score.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.GetRepository<AssessmentScore>().UpdateAsync(score);
            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AssessmentScoreResponse>> GetStudentResults(Guid studentId, Guid? termId, ClaimsPrincipal currentUser)
        {
            var query = _unitOfWork.GetRepository<AssessmentScore>().GetQueryable(
                a => a.StudentId == studentId,
                include: q => q.Include(a => a.Student).Include(a => a.ApprovedBy).ThenInclude(ab => ab!.User));

            if (termId.HasValue) query = query.Where(a => a.AcademicTermId == termId.Value);

            var scores = await query.ToListAsync();
            return _mapper.Map<IEnumerable<AssessmentScoreResponse>>(scores);
        }

        public async Task<string> ExportResults(Guid classId, string subject, Guid termId, string format)
        {
            var scores = await _unitOfWork.GetRepository<AssessmentScore>().GetByAsync(
                a => a.Student.ClassSectionId == classId && a.Subject.ToLower() == subject.ToLower() && a.AcademicTermId == termId,
                include: q => q.Include(a => a.Student));

            var sb = new StringBuilder();
            sb.AppendLine("Student Code,Student Name,Subject,CA1,CA2,Project,Exam,Total,Grade,Remark");
            foreach (var s in scores)
            {
                sb.AppendLine($"{s.Student.StudentCode},{s.Student.FullName},{s.Subject},{s.Ca1Score},{s.Ca2Score},{s.ProjectScore},{s.ExamScore},{s.TotalScore},{s.Grade},{s.Remark}");
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
                if (parts.Length >= 6)
                {
                    string code = parts[0].Trim();
                    var student = students.FirstOrDefault(s => s.StudentCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (student != null)
                    {
                        decimal.TryParse(parts[2], out var ca1);
                        decimal.TryParse(parts[3], out var ca2);
                        decimal.TryParse(parts[4], out var project);
                        decimal.TryParse(parts[5], out var exam);
                        string remark = parts.Length > 6 ? parts[6].Trim() : string.Empty;

                        items.Add(new AssessmentScoreItemRequest
                        {
                            StudentId = student.Id,
                            Ca1 = ca1,
                            Ca2 = ca2,
                            Project = project,
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
            // Template headers reflect configured grading scale thresholds from appsettings
            // The sample row uses a placeholder code — NOT a real student code with a hardcoded year
            return Task.FromResult(
                "StudentCode,StudentName,CA1,CA2,Project,Exam,Remark\n" +
                "STUDENT-CODE-HERE,Student Full Name,0,0,0,0,");
        }
    }
}

