using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class AssessmentScoreResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public decimal? Ca1Score { get; set; }
        public decimal? Ca2Score { get; set; }
        public decimal? ProjectScore { get; set; }
        public decimal? ExamScore { get; set; }
        public decimal TotalScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public AssessmentStatus Status { get; set; }
        public string? ApprovedByName { get; set; }
        public Guid AcademicTermId { get; set; }
        public string AcademicTermName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public int? TermNumber { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ClassTeacherName { get; set; }
        public string? ClassTeacherRemark { get; set; }
        public string? HosName { get; set; }
        public string? HosRemark { get; set; }
    }

    public class BatchUpdateAssessmentsResponse
    {
        public int Saved { get; set; }
        public List<ComputedScoreResponse> Computed { get; set; } = new List<ComputedScoreResponse>();
    }

    public class ComputedScoreResponse
    {
        public Guid StudentId { get; set; }
        public decimal Total { get; set; }
        public string Grade { get; set; } = string.Empty;
    }
}
