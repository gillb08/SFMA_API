using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class ExecutiveDashboardResponse
    {
        public int TotalEnrollment { get; set; }
        public decimal TotalRevenue { get; set; }
        public int RequisitionPipelineCount { get; set; }
        public int StaffCount { get; set; }
    }

public class EarlyYearsSnapshot
    {
        public string Focus { get; set; } = "Early Literacy & Motor Skills";
        public decimal MilestoneAttainmentRate { get; set; } = 96.2m;
        public int EnrolledPupils { get; set; } = 78;
    }

public class JuniorBasicSnapshot
    {
        public string Focus { get; set; } = "Cognitive & Terminal Assessment Broadsheet";
        public decimal AverageScore { get; set; } = 85.7m;
        public decimal PassRate { get; set; } = 100m;
        public int EvaluatedScholars { get; set; } = 6;
    }

public class QualityAssuranceSnapshot
    {
        public decimal SchemesVettedRate { get; set; } = 100m;
        public string Standing { get; set; } = "Approved for Terminal Examinations";
    }

public class AcademicSnapshotResponse
    {
        public string Term { get; set; } = "Term 2 2025/2026";
        public EarlyYearsSnapshot EarlyYears { get; set; } = new EarlyYearsSnapshot();
        public JuniorBasicSnapshot JuniorBasic { get; set; } = new JuniorBasicSnapshot();
        public QualityAssuranceSnapshot QualityAssurance { get; set; } = new QualityAssuranceSnapshot();
    }

public class BudgetItemProgress
    {
        public string Category { get; set; } = string.Empty;
        public decimal BudgetedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal PercentageUsed => BudgetedAmount == 0 ? 0 : Math.Round((ActualAmount / BudgetedAmount) * 100, 1);
    }

public class FinancialDashboardResponse
    {
        public decimal TotalBilled { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int UnpostedTellersCount { get; set; }
    }
}