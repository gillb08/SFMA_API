using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class LessonNoteResponse
    {
        public Guid Id { get; set; }
        public string NoteCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public string? Objectives { get; set; }
        public string? FileUrl { get; set; }
        public LessonNoteStatus Status { get; set; }
        public string? ReviewedByName { get; set; }
        public string? ReviewerComments { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}