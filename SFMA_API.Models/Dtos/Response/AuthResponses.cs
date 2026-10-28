using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class LinkedWardSummaryResponse
    {
        public Guid Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Guid ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public StudentStatus Status { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class UserProfileResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string PortalRedirect { get; set; } = string.Empty;
        public Guid? AssignedClassId { get; set; }
        public string? AssignedClassName { get; set; }
        public Guid? StaffId { get; set; }
        public Guid? StudentId { get; set; }
        public string? StudentCode { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string? ClassSectionName { get; set; }
        public List<LinkedWardSummaryResponse> LinkedWards { get; set; } = new List<LinkedWardSummaryResponse>();
    }

    public class LoggedInUserResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public UserProfileResponse User { get; set; } = null!;
        public List<string> MenuItems { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public AcademicTermResponse? ActiveTerm { get; set; }
    }
}
