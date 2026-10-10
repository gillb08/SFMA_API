using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class RequisitionResponse
    {
        public Guid Id { get; set; }
        public string RequisitionCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public RequisitionPriority Priority { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public RequisitionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ApprovalStageResponse> ApprovalStages { get; set; } = new List<ApprovalStageResponse>();
    }

public class ApprovalStageResponse
    {
        public Guid Id { get; set; }
        public ApprovalStageType Stage { get; set; }
        public ApprovalAction Action { get; set; }
        public string? ActedByName { get; set; }
        public DateTime? ActedAt { get; set; }
        public string? Notes { get; set; }
    }
}