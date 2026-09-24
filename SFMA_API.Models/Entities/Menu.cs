using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Entities
{
    public class Menu : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public int Order { get; set; }
        public string? ClaimsJson { get; set; }
    }
}
