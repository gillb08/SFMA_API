using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class CreateLessonNoteRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public string? Objectives { get; set; }
        public string? FileUrl { get; set; }
    }

public class ReviewLessonNoteRequest
    {
        public LessonNoteStatus Status { get; set; }
        public string? ReviewerComments { get; set; }
    }
}