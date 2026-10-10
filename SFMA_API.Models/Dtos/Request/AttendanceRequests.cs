using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Request
{
public class AttendanceRecordItemRequest
    {
        public Guid StudentId { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string? Notes { get; set; }
    }

public class BatchUpdateAttendanceRequest
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public List<AttendanceRecordItemRequest> Records { get; set; } = new List<AttendanceRecordItemRequest>();
    }

public class LockAttendanceRequest
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
    }

public class UnlockAttendanceRequest
    {
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

public class AbsenceNoticeRequest
    {
        public Guid StudentId { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}