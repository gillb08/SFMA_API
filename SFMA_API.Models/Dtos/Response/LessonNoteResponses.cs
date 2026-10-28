using SFMA_API.Models.Enums;
using System;

namespace SFMA_API.Models.Dtos.Response
{
    public class LessonNoteResponse
    {
        public Guid Id { get; set; }
        public string NoteCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public Guid? SubjectId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public Guid? AcademicTermId { get; set; }
        public string AcademicTermName { get; set; } = string.Empty;
        public Guid? SchemeOfWorkEntryId { get; set; }
        public short? WeekNumber { get; set; }
        public DateTime? LessonDate { get; set; }
        public int? PeriodNumber { get; set; }
        public string? Objectives { get; set; }
        public string? FileUrl { get; set; }
        public LessonNoteStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public LessonDeliveryStatus DeliveryStatus { get; set; }
        public string DeliveryStatusLabel { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryRemark { get; set; }
        public Guid? ReviewedById { get; set; }
        public string? ReviewedByName { get; set; }
        public string? ReviewerComments { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
