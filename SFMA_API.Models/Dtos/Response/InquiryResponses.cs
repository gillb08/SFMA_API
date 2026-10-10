using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class InquiryResponse
    {
        public Guid Id { get; set; }
        public string InquiryCode { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public Guid? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? ChildName { get; set; }
        public string? ClassApplying { get; set; }
        public string? Message { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public Guid? RoutedToId { get; set; }
        public string? RoutedToName { get; set; }
        public InquiryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InquiryMessageResponse> Messages { get; set; } = new List<InquiryMessageResponse>();
        public List<InquiryTimelineStepResponse> TimelineSteps { get; set; } = new List<InquiryTimelineStepResponse>();
    }

    public class InquiryMessageResponse
    {
        public Guid Id { get; set; }
        public string? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderRole { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class InquiryTimelineStepResponse
    {
        public Guid Id { get; set; }
        public string StageLabel { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public short SortOrder { get; set; }
    }
}
