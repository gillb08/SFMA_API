using SFMA_API.Models.Enums;
using System;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateSchemeOfWorkRequest
    {
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? CurriculumTopicId { get; set; }
        public short WeekNumber { get; set; }
        public string? WeekLabel { get; set; }
        public string? Topic { get; set; }
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus? Status { get; set; }
    }

    public class UpdateSchemeOfWorkRequest
    {
        public Guid? SubjectId { get; set; }
        public Guid? CurriculumTopicId { get; set; }
        public short? WeekNumber { get; set; }
        public string? WeekLabel { get; set; }
        public string? Topic { get; set; }
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus? Status { get; set; }
    }

    public class ReviewSchemeOfWorkRequest
    {
        public SchemeApprovalStatus Action { get; set; }
        public string? ReviewComment { get; set; }
    }
}
