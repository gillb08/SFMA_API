using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class SchemeOfWorkResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public Guid AcademicTermId { get; set; }
        public short WeekNumber { get; set; }
        public string WeekLabel { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? LearningObjectives { get; set; }
        public string? MaterialsName { get; set; }
        public string? MaterialsUrl { get; set; }
        public SchemeStatus Status { get; set; }
        public bool VettedByHos { get; set; }
    }
}