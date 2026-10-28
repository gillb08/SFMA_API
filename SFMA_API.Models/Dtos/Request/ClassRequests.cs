using System;
using System.ComponentModel.DataAnnotations;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateClassSectionRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Division { get; set; } = string.Empty;

        public Guid? HomeroomTeacherId { get; set; }

        public Guid? AcademicTermId { get; set; }
    }

    public class UpdateClassSectionRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Division { get; set; } = string.Empty;

        public Guid? HomeroomTeacherId { get; set; }

        public Guid? AcademicTermId { get; set; }
    }

    public class CreateSubjectRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Category { get; set; }

        public string? Division { get; set; }

        public string? Department { get; set; }

        public Guid? ClassSectionId { get; set; }

        public Guid? AcademicTermId { get; set; }

        public short? SortOrder { get; set; }

        public bool? Active { get; set; }
    }

    public class UpdateSubjectRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Category { get; set; }

        public string? Division { get; set; }

        public string? Department { get; set; }

        public Guid? ClassSectionId { get; set; }

        public Guid? AcademicTermId { get; set; }

        public short? SortOrder { get; set; }

        public bool? Active { get; set; }
    }
}
