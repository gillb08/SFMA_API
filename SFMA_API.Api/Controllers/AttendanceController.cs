using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/attendance")]
    [Authorize(Policy = "Authorization")]
    public class AttendanceController : BaseController
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet("", Name = "get-daily-attendance")]
        [SwaggerOperation(Summary = "Get daily attendance for class")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAttendance([FromQuery] Guid classId, [FromQuery] DateTime date)
        {
            var result = await _attendanceService.GetAttendance(classId, date, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("student/{studentId:guid}/monthly", Name = "get-student-monthly-attendance")]
        [SwaggerOperation(Summary = "Get student monthly attendance summary")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentMonthlyAttendance(Guid studentId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await _attendanceService.GetStudentMonthlyAttendance(studentId, month, year, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("batch", Name = "batch-mark-attendance")]
        [SwaggerOperation(Summary = "Batch mark attendance for class (Enforces 423 Locked if past 10:00 AM)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateAttendance([FromBody] BatchUpdateAttendanceRequest request)
        {
            var result = await _attendanceService.BatchUpdateAttendance(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("lock", Name = "lock-attendance-register")]
        [SwaggerOperation(Summary = "Manually lock attendance register")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LockAttendance([FromBody] LockAttendanceRequest request)
        {
            var result = await _attendanceService.LockAttendance(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("unlock", Name = "unlock-attendance-register")]
        [SwaggerOperation(Summary = "Unlock attendance register with supervisor audit logging")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlockAttendance([FromBody] UnlockAttendanceRequest request)
        {
            var result = await _attendanceService.UnlockAttendance(request, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("audit-logs", Name = "get-attendance-audit-logs")]
        [SwaggerOperation(Summary = "Get attendance unlock audit logs")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] Guid? classSectionId, [FromQuery] DateTime? date)
        {
            var result = await _attendanceService.GetAuditLogs(classSectionId, date);
            return Ok(result);
        }

        [HttpGet("export-csv", Name = "export-attendance-csv")]
        [SwaggerOperation(Summary = "Export attendance as CSV")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportAttendanceCsv([FromQuery] Guid classId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var csv = await _attendanceService.ExportAttendanceCsv(classId, startDate, endDate);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "attendance_export.csv");
        }

        [HttpPost("absence-notice", Name = "submit-absence-notice")]
        [SwaggerOperation(Summary = "Submit absence notice for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitAbsenceNotice([FromBody] AbsenceNoticeRequest request)
        {
            var result = await _attendanceService.SubmitAbsenceNotice(request, HttpContext.User);
            return Ok(result);
        }
    }
}

