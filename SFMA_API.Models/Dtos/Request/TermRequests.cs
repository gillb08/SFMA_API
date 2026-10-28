using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateTermRequest : IValidatableObject
    {
        [Required(ErrorMessage = "SessionName is required.")]
        [StringLength(50, ErrorMessage = "SessionName cannot exceed 50 characters.")]
        public string SessionName { get; set; } = string.Empty;

        [Range(1, 3, ErrorMessage = "TermNumber must be between 1 and 3.")]
        public short TermNumber { get; set; }

        [Required(ErrorMessage = "StartDate is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "EndDate is required.")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult("EndDate must be after StartDate.", new[] { nameof(EndDate), nameof(StartDate) });
            }
        }
    }

    public class UpdateTermRequest : IValidatableObject
    {
        [Required(ErrorMessage = "SessionName is required.")]
        [StringLength(50, ErrorMessage = "SessionName cannot exceed 50 characters.")]
        public string SessionName { get; set; } = string.Empty;

        [Range(1, 3, ErrorMessage = "TermNumber must be between 1 and 3.")]
        public short TermNumber { get; set; }

        [Required(ErrorMessage = "StartDate is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "EndDate is required.")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult("EndDate must be after StartDate.", new[] { nameof(EndDate), nameof(StartDate) });
            }
        }
    }

    public class ConfigureDeadlinesRequest
    {
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }

    public class UpdateTermDeadlinesRequest : ConfigureDeadlinesRequest
    {
    }
}