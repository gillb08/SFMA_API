using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class CreateRequisitionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequisitionPriority Priority { get; set; } = RequisitionPriority.Medium;
    }

public class ApproveRequisitionRequest
    {
        public string? Notes { get; set; }
    }

public class QueryRequisitionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}