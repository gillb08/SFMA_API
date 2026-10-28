using System;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateCurriculumTopicRequest
    {
        public Guid SubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string? LearningOutcomes { get; set; }
        public short SequenceNumber { get; set; } = 1;
    }

    public class UpdateCurriculumTopicRequest
    {
        public string? Topic { get; set; }
        public string? LearningOutcomes { get; set; }
        public short? SequenceNumber { get; set; }
        public bool? Active { get; set; }
    }
}
