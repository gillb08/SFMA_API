using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class InquiryResponse
    {
        public Guid Id { get; set; }
        public string InquiryCode { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public Guid RoutedToId { get; set; }
        public string RoutedToName { get; set; } = string.Empty;
        public InquiryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InquiryMessageResponse> Messages { get; set; } = new List<InquiryMessageResponse>();
        public List<InquiryTimelineStepResponse> TimelineSteps { get; set; } = new List<InquiryTimelineStepResponse>();
    }

public class InquiryMessageResponse
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
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