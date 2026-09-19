using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Entities
{
    public class AcademicTerm : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SessionName { get; set; } = string.Empty;
        public short TermNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }

        public virtual ICollection<ClassSection> ClassSections { get; set; } = new List<ClassSection>();
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public virtual ICollection<AssessmentScore> AssessmentScores { get; set; } = new List<AssessmentScore>();
        public virtual ICollection<FeeSchedule> FeeSchedules { get; set; } = new List<FeeSchedule>();
        public virtual ICollection<FeeTransaction> FeeTransactions { get; set; } = new List<FeeTransaction>();
        public virtual ICollection<SchemeOfWorkEntry> SchemeOfWorkEntries { get; set; } = new List<SchemeOfWorkEntry>();
        public virtual ICollection<SchoolRequirement> SchoolRequirements { get; set; } = new List<SchoolRequirement>();
    }

    public class ClassSection : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public Guid? HomeroomTeacherId { get; set; }
        public Guid AcademicTermId { get; set; }

        public virtual Staff? HomeroomTeacher { get; set; }
        public virtual AcademicTerm AcademicTerm { get; set; } = null!;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
        public virtual ICollection<SchemeOfWorkEntry> SchemeOfWorkEntries { get; set; } = new List<SchemeOfWorkEntry>();
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public virtual ICollection<LessonNote> LessonNotes { get; set; } = new List<LessonNote>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<SchoolRequirement> SchoolRequirements { get; set; } = new List<SchoolRequirement>();
        public virtual ICollection<AttendanceAuditLog> AttendanceAuditLogs { get; set; } = new List<AttendanceAuditLog>();
    }

    public class AttendanceRecord : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public Guid ClassSectionId { get; set; }
        public DateTime Date { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string? Notes { get; set; }
        public Guid RecordedById { get; set; }
        public bool Locked { get; set; }
        public Guid AcademicTermId { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual Staff RecordedBy { get; set; } = null!;
        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
    }

    public class AttendanceAuditLog : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClassSectionId { get; set; }
        public DateTime Date { get; set; }
        public Guid UnlockedById { get; set; }
        public string RoleAtAction { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = "Register Unlocked for Roll Correction";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual Staff UnlockedBy { get; set; } = null!;
    }

    public class AssessmentScore : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public decimal? Ca1Score { get; set; }
        public decimal? Ca2Score { get; set; }
        public decimal? ProjectScore { get; set; }
        public decimal? ExamScore { get; set; }
        public decimal TotalScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public Guid AcademicTermId { get; set; }
        public Guid SubmittedById { get; set; }
        public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
        public Guid? ApprovedById { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
        public virtual Staff SubmittedBy { get; set; } = null!;
        public virtual Staff? ApprovedBy { get; set; }
    }

    public class SchemeOfWorkEntry : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public short WeekNumber { get; set; }
        public string WeekLabel { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus Status { get; set; } = SchemeStatus.Upcoming;
        public bool VettedByHos { get; set; }

        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
    }

    public class TimetableSlot : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClassSectionId { get; set; }
        public DayOfWeekEnum DayOfWeek { get; set; }
        public short PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Subject { get; set; }
        public Guid? TeacherId { get; set; }
        public bool IsBreak { get; set; }

        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual Staff? Teacher { get; set; }
    }

    public class LessonNote : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NoteCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public string? Objectives { get; set; }
        public string? FileUrl { get; set; }
        public LessonNoteStatus Status { get; set; } = LessonNoteStatus.Submitted;
        public Guid? ReviewedById { get; set; }
        public string? ReviewerComments { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public virtual Staff Staff { get; set; } = null!;
        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual Staff? ReviewedBy { get; set; }
    }

    public class Assignment : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AssignmentCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime DueDate { get; set; }
        public string SubmissionMode { get; set; } = "Any File Format";
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Published;

        public virtual Staff Staff { get; set; } = null!;
        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }

    public class AssignmentSubmission : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? FileUrl { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string? Grade { get; set; }

        public virtual Assignment Assignment { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
    }
}
