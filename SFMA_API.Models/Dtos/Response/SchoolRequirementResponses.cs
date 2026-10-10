using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class SchoolRequirementResponse
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public bool IsRequired { get; set; }
        public bool? Acquired { get; set; }
    }
}