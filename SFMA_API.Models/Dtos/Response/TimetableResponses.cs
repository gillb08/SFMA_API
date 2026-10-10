using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class TimetableSlotResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public DayOfWeekEnum DayOfWeek { get; set; }
        public short PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Subject { get; set; }
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public bool IsBreak { get; set; }
    }
}