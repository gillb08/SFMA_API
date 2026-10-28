using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
    public class TermResponse
    {
        public Guid Id { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public short TermNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }

    public class AcademicTermResponse : TermResponse
    {
    }

    public class DeadlinesResponse
    {
        public bool Success { get; set; } = true;
        public Guid TermId { get; set; }
        public DeadlinesDto Deadlines { get; set; } = new DeadlinesDto();
    }

    public class DeadlinesDto
    {
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }
}
