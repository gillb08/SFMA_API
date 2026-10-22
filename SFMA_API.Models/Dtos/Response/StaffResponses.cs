using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class StaffResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public Guid? AssignedClassId { get; set; }
        public string? AssignedClassName { get; set; }
    }

public class CreateStaffResponse
    {
        public StaffResponse Staff { get; set; } = null!;
        public UserProfileResponse User { get; set; } = null!;
        public string TempPassword { get; set; } = string.Empty;
    }
}