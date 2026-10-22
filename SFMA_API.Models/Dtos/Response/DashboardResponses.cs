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
        public string Focus { get; set; } = string.Empty;
        public decimal MilestoneAttainmentRate { get; set; }
        public int EnrolledPupils { get; set; }
    }

public class JuniorBasicSnapshot
    {
        public string Focus { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal PassRate { get; set; }
        public int EvaluatedScholars { get; set; }
    }

public class QualityAssuranceSnapshot
    {
        public decimal SchemesVettedRate { get; set; }
        public string Standing { get; set; } = string.Empty;
    }

public class AcademicSnapshotResponse
    {
        public string Term { get; set; } = string.Empty;
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