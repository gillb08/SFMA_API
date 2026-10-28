using SFMA_API.Models.Enums;
using System;

namespace SFMA_API.Models.Dtos.Response
{
    public class SchemeOfWorkResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public Guid AcademicTermId { get; set; }
        public string AcademicTermName { get; set; } = string.Empty;
        public Guid? SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public Guid? CurriculumTopicId { get; set; }
        public string? CurriculumTopicTitle { get; set; }
        public short WeekNumber { get; set; }
        public string WeekLabel { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus Status { get; set; }
        public SchemeApprovalStatus ApprovalStatus { get; set; }
        public string ApprovalStatusLabel { get; set; } = string.Empty;
        public bool VettedByHos { get; set; }
        public Guid? CreatedByStaffId { get; set; }
        public string? CreatedByStaffName { get; set; }
        public Guid? VettedByStaffId { get; set; }
        public string? VettedByStaffName { get; set; }
        public DateTime? VettedAt { get; set; }
        public string? ReviewComment { get; set; }
        public int DeliveredLessonCount { get; set; }
        public int TotalLessonCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
