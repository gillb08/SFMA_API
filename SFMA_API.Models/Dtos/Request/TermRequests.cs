using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class UpdateTermRequest
    {
        public string SessionName { get; set; } = string.Empty;
        public short TermNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

public class ConfigureDeadlinesRequest
    {
        public DateTime? Cat1Deadline { get; set; }
        public DateTime? Cat2Deadline { get; set; }
        public DateTime? ExamDeadline { get; set; }
    }
}