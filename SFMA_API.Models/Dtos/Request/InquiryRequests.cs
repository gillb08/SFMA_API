using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class CreateInquiryRequest
    {
        public Guid StudentId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SubjectLine { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
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