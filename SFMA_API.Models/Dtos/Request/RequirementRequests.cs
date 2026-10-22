using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SFMA_API.Models.Dtos.Request
{
    public class ToggleRequirementStatusRequest
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public Guid StudentId { get; set; }

        public bool Acquired { get; set; }
    }

    public class UpsertSchoolRequirementRequest : IValidatableObject
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "ClassSectionId is required.")]
        public Guid ClassSectionId { get; set; }

        public string? ClassName { get; set; }

        public Guid? AcademicTermId { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters.")]
        public string? Author { get; set; }

        [StringLength(200, ErrorMessage = "Publisher cannot exceed 200 characters.")]
        public string? Publisher { get; set; }

        public bool IsRequired { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ClassSectionId == Guid.Empty)
            {
                yield return new ValidationResult("ClassSectionId cannot be empty.", new[] { nameof(ClassSectionId) });
            }
        }
    }
}