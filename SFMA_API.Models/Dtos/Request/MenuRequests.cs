using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class CreateMenuRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public List<string> Claims { get; set; } = new List<string>();
    }

public class UpdateMenuRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public bool Active { get; set; } = true;
        public List<string> Claims { get; set; } = new List<string>();
    }

public class AddClaimsToMenuRequest
    {
        public Guid MenuId { get; set; }
        public List<string> Claims { get; set; } = new List<string>();
    }
}