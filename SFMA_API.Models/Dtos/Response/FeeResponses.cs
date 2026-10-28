using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class FeeItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public short SortOrder { get; set; }
    }

    public class FeeScheduleResponse
    {
        public Guid Id { get; set; }
        public Guid AcademicTermId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<FeeItemResponse> FeeItems { get; set; } = new List<FeeItemResponse>();
    }

    public class FeeTransactionResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public Guid AcademicTermId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? BankTellerRef { get; set; }
        public string? BankName { get; set; }
        public string? AttachmentFile { get; set; }
        public string? AttachmentUrl { get; set; }
        public FeeTransactionStatus Status { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StudentFeeSummaryResponse
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string ClassSectionName { get; set; } = string.Empty;
        public Guid? AcademicTermId { get; set; }
        public string AcademicTermName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public int? TermNumber { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PendingReviewAmount { get; set; }
        public int PendingTellersCount { get; set; }
        public decimal Balance { get; set; }
        public string ClearanceStatus { get; set; } = "Unbilled";
        public List<FeeItemResponse> FeeItems { get; set; } = new List<FeeItemResponse>();
        public List<FeeItemResponse> Breakdown { get; set; } = new List<FeeItemResponse>();
        public List<FeeTransactionResponse> Transactions { get; set; } = new List<FeeTransactionResponse>();
    }

    public class ReceiptResponse
    {
        public Guid Id { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public Guid FeeTransactionId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string VerifiedByName { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? BankTellerRef { get; set; }
        public FeeTransactionStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? AttachmentUrl { get; set; }
        public string AcademicTermName { get; set; } = string.Empty;
        public bool PdfAvailable { get; set; }
        public string? PdfUrl { get; set; }
    }

    public class BankAccountResponse
    {
        public Guid Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string SortCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
