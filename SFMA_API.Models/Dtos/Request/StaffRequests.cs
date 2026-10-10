using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class CreateStaffRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleKey { get; set; } = "teacher";
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

public class UpdateStaffRequest
    {
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? RoleKey { get; set; }
        public string? Phone { get; set; }
    }

public class UpdateStaffStatusRequest
    {
        public string Status { get; set; } = "active";
    }
}