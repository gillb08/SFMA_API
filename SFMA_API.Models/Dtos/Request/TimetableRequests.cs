using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class TimetableSlotRequest
    {
        public DayOfWeekEnum DayOfWeek { get; set; }
        public short PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Subject { get; set; }
        public Guid? TeacherId { get; set; }
        public bool IsBreak { get; set; }
    }

public class UpdateTimetableRequest
    {
        public List<TimetableSlotRequest> Slots { get; set; } = new List<TimetableSlotRequest>();
    }
}