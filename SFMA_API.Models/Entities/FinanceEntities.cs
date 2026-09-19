using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Entities
{
    public class FeeSchedule : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AcademicTermId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public decimal TotalAmount { get; set; }

        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
        public virtual ClassSection? ClassSection { get; set; }
        public virtual ICollection<FeeItem> FeeItems { get; set; } = new List<FeeItem>();
    }

    public class FeeItem : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FeeScheduleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public short SortOrder { get; set; }

        public virtual FeeSchedule FeeSchedule { get; set; } = null!;
    }

    public class FeeTransaction : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public Guid AcademicTermId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public string? BankTellerRef { get; set; }
        public FeeTransactionStatus Status { get; set; } = FeeTransactionStatus.PendingReview;
        public Guid? VerifiedById { get; set; }
        public string? Notes { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
        public virtual Staff? VerifiedBy { get; set; }
        public virtual Receipt? Receipt { get; set; }
    }

    public class Receipt : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ReceiptCode { get; set; } = string.Empty;
        public Guid FeeTransactionId { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public Guid VerifiedById { get; set; }
        public string? PdfUrl { get; set; }

        public virtual FeeTransaction FeeTransaction { get; set; } = null!;
        public virtual Staff VerifiedBy { get; set; } = null!;
    }

    public class Requisition : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RequisitionCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequisitionPriority Priority { get; set; } = RequisitionPriority.Medium;
        public Guid RequestedById { get; set; }
        public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;

        public virtual Staff RequestedBy { get; set; } = null!;
        public virtual ICollection<ApprovalStage> ApprovalStages { get; set; } = new List<ApprovalStage>();
    }

    public class ApprovalStage : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RequisitionId { get; set; }
        public ApprovalStageType Stage { get; set; }
        public ApprovalAction Action { get; set; } = ApprovalAction.Pending;
        public Guid? ActedById { get; set; }
        public DateTime? ActedAt { get; set; }
        public string? Notes { get; set; }

        public virtual Requisition Requisition { get; set; } = null!;
        public virtual Staff? ActedBy { get; set; }
    }

    public class BankAccount : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string SortCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Stores the approved budget allocation per department per academic term.
    /// Used by the budget progress dashboard to compare actual spend against planned budget.
    /// Replace the old hardcoded 500,000 fallback.
    /// </summary>
    public class DepartmentBudget : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Department { get; set; } = string.Empty;
        public Guid AcademicTermId { get; set; }
        public decimal AllocatedAmount { get; set; }

        public virtual AcademicTerm AcademicTerm { get; set; } = null!;
    }
}
