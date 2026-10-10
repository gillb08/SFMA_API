using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class UpdateRoleClaimsRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public string? RoleKey { get; set; }
        public List<string> Claims { get; set; } = new List<string>();
        public bool Active { get; set; } = true;
    }

public class UpdateUserClaimsRequest
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> Claims { get; set; } = new List<string>();
        public bool Active { get; set; } = true;
    }
}