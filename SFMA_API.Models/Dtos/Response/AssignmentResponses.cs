using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class AssignmentResponse
    {
        public Guid Id { get; set; }
        public string AssignmentCode { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime DueDate { get; set; }
        public string SubmissionMode { get; set; } = "Any File Format";
        public AssignmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SubmissionsCount { get; set; }
    }

public class AssignmentSubmissionResponse
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? Grade { get; set; }
    }
}