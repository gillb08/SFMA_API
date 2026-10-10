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
}