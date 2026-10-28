using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
    public class AssessmentScoreItemRequest
    {
        public Guid StudentId { get; set; }
        public decimal? Ca1 { get; set; }
        public decimal? Ca2 { get; set; }
        public decimal? Project { get; set; }
        public decimal? Exam { get; set; }
        public string? Remark { get; set; }
    }

    public class BatchUpdateAssessmentsRequest
    {
        public Guid ClassId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public List<AssessmentScoreItemRequest> Scores { get; set; } = new List<AssessmentScoreItemRequest>();
    }

    public class BroadsheetActionRequest
    {
        public Guid ClassId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public string? Remark { get; set; }
    }
}
