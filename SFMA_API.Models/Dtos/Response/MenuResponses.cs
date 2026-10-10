using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class MenuResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public List<string> Claims { get; set; } = new List<string>();
        public bool Active { get; set; }
    }
}