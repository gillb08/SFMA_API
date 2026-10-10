using SFMA_API.Models.Enums;
using System;
using System.Collections.Generic;

namespace SFMA_API.Models.Dtos.Response
{
public class AttendanceRecordResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public Guid ClassSectionId { get; set; }
        public DateTime Date { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string? Notes { get; set; }
        public string RecordedByName { get; set; } = string.Empty;
        public bool Locked { get; set; }
    }

public class BatchUpdateAttendanceResponse
    {
        public int Updated { get; set; }
        public bool Synced { get; set; }
    }

public class UnlockAttendanceResponse
    {
        public bool Success { get; set; } = true;
        public Guid ClassId { get; set; }
        public DateTime Date { get; set; }
        public string UnlockedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

public class AttendanceAuditLogResponse
    {
        public Guid Id { get; set; }
        public Guid ClassSectionId { get; set; }
        public DateTime Date { get; set; }
        public string UnlockedByName { get; set; } = string.Empty;
        public string RoleAtAction { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}