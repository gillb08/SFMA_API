using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Entities
{
    public class Staff : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<LessonNote> LessonNotes { get; set; } = new List<LessonNote>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Requisition> Requisitions { get; set; } = new List<Requisition>();
    }

    public class Student : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Guid ClassSectionId { get; set; }
        public string? PhotoUrl { get; set; }
        public StudentStatus Status { get; set; } = StudentStatus.Admitted;
        public DateTime AdmittedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual IdCard? IdCard { get; set; }

        public virtual ICollection<StudentParent> StudentParents { get; set; } = new List<StudentParent>();
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public virtual ICollection<AssessmentScore> AssessmentScores { get; set; } = new List<AssessmentScore>();
        public virtual ICollection<FeeTransaction> FeeTransactions { get; set; } = new List<FeeTransaction>();
        public virtual ICollection<RequirementStatus> RequirementStatuses { get; set; } = new List<RequirementStatus>();
        public virtual ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
        public virtual ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
    }

    public class Parent : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<StudentParent> StudentParents { get; set; } = new List<StudentParent>();
        public virtual ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
    }

    public class StudentParent
    {
        public Guid StudentId { get; set; }
        public Guid ParentId { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Parent Parent { get; set; } = null!;
    }

    public class IdCard : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public string CardSerial { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool NfcProvisioned { get; set; } = true;
        public IdCardStatus Status { get; set; } = IdCardStatus.Generated;

        public virtual Student Student { get; set; } = null!;
    }
}
