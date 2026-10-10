using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class CreateAssignmentRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime DueDate { get; set; }
        public string SubmissionMode { get; set; } = "Any File Format";
    }

public class UpdateAssignmentRequest
    {
        public string? Subject { get; set; }
        public string? Title { get; set; }
        public string? Instructions { get; set; }
        public DateTime? DueDate { get; set; }
        public string? SubmissionMode { get; set; }
        public AssignmentStatus? Status { get; set; }
    }

public class SubmitAssignmentRequest
    {
        public Guid StudentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeBytes { get; set; }
        public string? FileUrl { get; set; }
    }

public class GradeAssignmentSubmissionRequest
    {
        public string Grade { get; set; } = string.Empty;
    }
}