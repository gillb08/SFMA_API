using System;
using System.ComponentModel.DataAnnotations;

namespace SFMA_API.Models.Dtos.Request
{
    public class CreateClassSectionRequest
    {
        [Required(ErrorMessage = "Class Name is required.")]
        [StringLength(100, ErrorMessage = "Class Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Division is required.")]
        [StringLength(100, ErrorMessage = "Division cannot exceed 100 characters.")]
        public string Division { get; set; } = string.Empty;

        public Guid? HomeroomTeacherId { get; set; }

        public Guid? AcademicTermId { get; set; }
    }

    public class UpdateClassSectionRequest
    {
        [Required(ErrorMessage = "Class Name is required.")]
        [StringLength(100, ErrorMessage = "Class Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Division is required.")]
        [StringLength(100, ErrorMessage = "Division cannot exceed 100 characters.")]
        public string Division { get; set; } = string.Empty;

        public Guid? HomeroomTeacherId { get; set; }

        public Guid? AcademicTermId { get; set; }
    }
}
