using SFMA_API.Models.Enums;
using System;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateLessonNoteRequest
    {
        public Guid SchemeOfWorkEntryId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public Guid? AcademicTermId { get; set; }
        public Guid? SubjectId { get; set; }
        public string? Subject { get; set; }
        public string? Topic { get; set; }
        public DateTime? LessonDate { get; set; }
        public int? PeriodNumber { get; set; }
        public string? Objectives { get; set; }
        public string? ContentSummary { get; set; }
        public string? FileUrl { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class ReviewLessonNoteRequest
    {
        public LessonNoteStatus Status { get; set; }
        public string? ReviewerComments { get; set; }
    }

    public class MarkLessonDeliveryRequest
    {
        public LessonDeliveryStatus DeliveryStatus { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryRemark { get; set; }
    }
}
