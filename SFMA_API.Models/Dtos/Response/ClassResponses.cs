using System;

namespace SFMA_API.Models.Dtos.Response
{
    public class ClassSectionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public Guid? HomeroomTeacherId { get; set; }
        public string? HomeroomTeacherName { get; set; }
        public Guid AcademicTermId { get; set; }
        public string? AcademicTermName { get; set; }
        public int StudentCount { get; set; }
    }
}
