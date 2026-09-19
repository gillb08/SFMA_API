using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Entities
{
    public class Inquiry : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string InquiryCode { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public Guid StudentId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public Guid RoutedToId { get; set; }
        public InquiryStatus Status { get; set; } = InquiryStatus.Open;

        public virtual Parent Parent { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
        public virtual Staff RoutedTo { get; set; } = null!;
        public virtual ICollection<InquiryMessage> Messages { get; set; } = new List<InquiryMessage>();
        public virtual ICollection<InquiryTimelineStep> TimelineSteps { get; set; } = new List<InquiryTimelineStep>();
    }

    public class InquiryMessage : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InquiryId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public virtual Inquiry Inquiry { get; set; } = null!;
        public virtual ApplicationUser Sender { get; set; } = null!;
    }

    public class InquiryTimelineStep : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid InquiryId { get; set; }
        public string StageLabel { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public short SortOrder { get; set; }

        public virtual Inquiry Inquiry { get; set; } = null!;
    }

    public class SchoolRequirement : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ItemCode { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public bool IsRequired { get; set; } = true;

        public virtual ClassSection ClassSection { get; set; } = null!;
        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
        public virtual ICollection<RequirementStatus> RequirementStatuses { get; set; } = new List<RequirementStatus>();
    }

    public class RequirementStatus : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RequirementId { get; set; }
        public Guid StudentId { get; set; }
        public bool Acquired { get; set; }
        public DateTime? AcquiredAt { get; set; }

        public virtual SchoolRequirement Requirement { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
    }
}
