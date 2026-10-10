using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
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
    }

public class LoggedInUserResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public UserProfileResponse User { get; set; } = null!;
        public List<string> MenuItems { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
    }
}