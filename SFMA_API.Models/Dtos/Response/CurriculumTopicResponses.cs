using System;

namespace SFMA_API.Models.Dtos.Response
{
    public class CurriculumTopicResponse
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public Guid? ClassSectionId { get; set; }
        public string? ClassSectionName { get; set; }
        public Guid AcademicTermId { get; set; }
        public string AcademicTermName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? LearningOutcomes { get; set; }
        public short SequenceNumber { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CurriculumCoverageReportResponse
    {
        public Guid ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int TotalCurriculumTopics { get; set; }
        public int PlannedSchemeWeeks { get; set; }
        public int ApprovedSchemeWeeks { get; set; }
        public int DeliveredLessons { get; set; }
        public decimal CoveragePercentage { get; set; }
        public int PendingReviews { get; set; }
        public int TopicsBehindScheduleCount { get; set; }
    }

    public class CurriculumAlertResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public Guid? TargetId { get; set; }
        public string? TargetTitle { get; set; }
    }
}
