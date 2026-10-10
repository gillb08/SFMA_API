using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateInquiryRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? RoutedToId { get; set; }

        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string? ChildName { get; set; }
        public string? ClassApplying { get; set; }

        public string Category { get; set; } = "Admissions";
        public string? SubjectLine { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }

    public class AddInquiryMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateInquiryStatusRequest
    {
        public InquiryStatus Status { get; set; }
    }
}
