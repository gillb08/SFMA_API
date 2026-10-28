using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class StudentResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Guid ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public string? ClassTeacherName { get; set; }
        public string? AcademicSessionName { get; set; }
        public string? PhotoUrl { get; set; }
        public StudentStatus Status { get; set; }
        public DateTime AdmittedAt { get; set; }
        public IdCardResponse? IdCard { get; set; }
        public List<StudentParentResponse> Parents { get; set; } = new List<StudentParentResponse>();
    }

    public class AdmitStudentResponse
    {
        public StudentResponse Student { get; set; } = null!;
        public UserProfileResponse ParentAccount { get; set; } = null!;
        public UserProfileResponse StudentAccount { get; set; } = null!;
        public IdCardResponse IdCard { get; set; } = null!;
    }

    public class StudentParentResponse
    {
        public Guid ParentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class IdCardResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string ClassSectionName { get; set; } = string.Empty;
        public string? ClassTeacherName { get; set; }
        public string? AcademicSessionName { get; set; }
        public string? PhotoUrl { get; set; }
        public string CardSerial { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool NfcProvisioned { get; set; } = true;
        public IdCardStatus Status { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}
