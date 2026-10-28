using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
    public class PostTellerRequest
    {
        public Guid StudentId { get; set; }
        public Guid? AcademicTermId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BankTellerRef { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        /// <summary>Payment method e.g. Bank Transfer, Mobile Transfer, POS, Cash</summary>
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public string? Notes { get; set; }
        public string? AttachmentFile { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class VerifyTellerRequest
    {
        public string Status { get; set; } = "verified";
        public string? Notes { get; set; }
    }

    public class QueuePaymentRemindersRequest
    {
        public Guid AcademicTermId { get; set; }
        public decimal? MinBalance { get; set; }
    }

    public class FeeItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public short SortOrder { get; set; }
    }

    public class UpsertFeeScheduleRequest
    {
        public Guid AcademicTermId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string? ClassName { get; set; }
        public List<FeeItemRequest> FeeItems { get; set; } = new List<FeeItemRequest>();
    }

    public class UpsertBankAccountRequest
    {
        public Guid? Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? SortCode { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
