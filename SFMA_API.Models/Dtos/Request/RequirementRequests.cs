using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class ToggleRequirementStatusRequest
    {
        public Guid StudentId { get; set; }
        public bool Acquired { get; set; }
    }
}