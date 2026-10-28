using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class ClassSectionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public Guid? HomeroomTeacherId { get; set; }
        public string? HomeroomTeacherName { get; set; }
        public Guid AcademicTermId { get; set; }
        public string? AcademicTermName { get; set; }
        public int StudentCount { get; set; }
    }

    public class SubjectResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Category { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string? ClassSectionName { get; set; }
        public Guid? AcademicTermId { get; set; }
        public short SortOrder { get; set; }
        public bool Active { get; set; } = true;
    }

    public class InquiryRecipientResponse
    {
        public Guid? StaffId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string RoleLabel { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public class SchoolConfigResponse
    {
        public string SchoolHours { get; set; } = string.Empty;
        public string AssemblyTime { get; set; } = string.Empty;
        public List<string> Departments { get; set; } = new List<string>();
        public List<string> Divisions { get; set; } = new List<string>();
        public List<string> SupportedFileFormats { get; set; } = new List<string>();
        public List<string> InquiryRoutingRoles { get; set; } = new List<string>();
        public List<InquiryRecipientResponse> InquiryRecipients { get; set; } = new List<InquiryRecipientResponse>();
        public TermResponse? ActiveTerm { get; set; }
    }

    public class UploadedFileResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
