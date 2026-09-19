using Microsoft.AspNetCore.Authorization;
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
    public class AttendanceController : BaseController
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get daily attendance for class")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAttendance([FromQuery] Guid classId, [FromQuery] DateTime date)
        {
            var result = await _attendanceService.GetAttendance(classId, date, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("student/{studentId:guid}/monthly")]
        [SwaggerOperation(Summary = "Get student monthly attendance summary")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentMonthlyAttendance(Guid studentId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await _attendanceService.GetStudentMonthlyAttendance(studentId, month, year, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad,teacher")]
        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Batch mark attendance for class (Enforces 423 Locked if past 10:00 AM)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateAttendance([FromBody] BatchUpdateAttendanceRequest request)
        {
            var result = await _attendanceService.BatchUpdateAttendance(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad,teacher")]
        [HttpPost("lock")]
        [SwaggerOperation(Summary = "Manually lock attendance register")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LockAttendance([FromBody] LockAttendanceRequest request)
        {
            var result = await _attendanceService.LockAttendance(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad")]
        [HttpPost("unlock")]
        [SwaggerOperation(Summary = "Unlock attendance register with supervisor audit logging")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlockAttendance([FromBody] UnlockAttendanceRequest request)
        {
            var result = await _attendanceService.UnlockAttendance(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad")]
        [HttpGet("audit-logs")]
        [SwaggerOperation(Summary = "Get attendance unlock audit logs")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] Guid? classSectionId, [FromQuery] DateTime? date)
        {
            var result = await _attendanceService.GetAuditLogs(classSectionId, date);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad,teacher")]
        [HttpGet("export-csv")]
        [SwaggerOperation(Summary = "Export attendance as CSV")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportAttendanceCsv([FromQuery] Guid classId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var csv = await _attendanceService.ExportAttendanceCsv(classId, startDate, endDate);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "attendance_export.csv");
        }

        [Authorize(Roles = "super_admin,parent,student")]
        [HttpPost("absence-notice")]
        [SwaggerOperation(Summary = "Submit absence notice for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitAbsenceNotice([FromBody] AbsenceNoticeRequest request)
        {
            var result = await _attendanceService.SubmitAbsenceNotice(request, HttpContext.User);
            return Ok(result);
        }
    }

    [Route("api/v1/assessments")]
    public class AssessmentsController : BaseController
    {
        private readonly IAssessmentService _assessmentService;

        public AssessmentsController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        [Authorize]
        [HttpGet("broadsheet")]
        [SwaggerOperation(Summary = "Get assessment broadsheet for class and subject")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBroadsheet([FromQuery] Guid classId, [FromQuery] string subject, [FromQuery] Guid termId)
        {
            var result = await _assessmentService.GetBroadsheet(classId, subject, termId, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad,teacher")]
        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Batch enter CA and Exam scores (Enforces 403 Forbidden on deadline elapsed)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateScores([FromBody] BatchUpdateAssessmentsRequest request)
        {
            var result = await _assessmentService.BatchUpdateScores(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad,teacher")]
        [HttpPost("submit")]
        [SwaggerOperation(Summary = "Submit broadsheet for approval")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitBroadsheet([FromBody] BroadsheetActionRequest request)
        {
            var result = await _assessmentService.SubmitBroadsheet(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad")]
        [HttpPost("approve")]
        [SwaggerOperation(Summary = "Approve and publish broadsheet")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveBroadsheet([FromBody] BroadsheetActionRequest request)
        {
            var result = await _assessmentService.ApproveBroadsheet(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("student/{studentId:guid}")]
        [SwaggerOperation(Summary = "Get terminal assessment results for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentResults(Guid studentId, [FromQuery] Guid? termId)
        {
            var result = await _assessmentService.GetStudentResults(studentId, termId, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("export")]
        [SwaggerOperation(Summary = "Export broadsheet results as CSV or PDF")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportResults([FromQuery] Guid classId, [FromQuery] string subject, [FromQuery] Guid termId, [FromQuery] string format = "csv")
        {
            var content = await _assessmentService.ExportResults(classId, subject, termId, format);
            var bytes = Encoding.UTF8.GetBytes(content);
            return File(bytes, "text/csv", "broadsheet_results.csv");
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad,teacher")]
        [HttpPost("import-csv")]
        [SwaggerOperation(Summary = "Import assessment scores via CSV string payload")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportScoresCsv([FromQuery] Guid classId, [FromQuery] string subject, [FromQuery] Guid termId, [FromBody] string csvContent)
        {
            var result = await _assessmentService.ImportScoresCsv(classId, subject, termId, csvContent, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("template")]
        [SwaggerOperation(Summary = "Download CSV template for scores upload")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetScoreTemplateCsv()
        {
            var template = await _assessmentService.GetScoreTemplateCsv();
            var bytes = Encoding.UTF8.GetBytes(template);
            return File(bytes, "text/csv", "score_import_template.csv");
        }
    }

    [Route("api/v1/fees")]
    public class FeesController : BaseController
    {
        private readonly IFeeService _feeService;

        public FeesController(IFeeService feeService)
        {
            _feeService = feeService;
        }

        [Authorize]
        [HttpGet("schedule")]
        [SwaggerOperation(Summary = "Get fee schedule for term and class")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeeSchedule([FromQuery] Guid termId, [FromQuery] Guid? classSectionId)
        {
            var result = await _feeService.GetFeeSchedule(termId, classSectionId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,bursar")]
        [HttpGet("ledger")]
        [SwaggerOperation(Summary = "Get fee ledger transactions with filters")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeeLedger([FromQuery] FeeTransactionStatus? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _feeService.GetFeeLedger(status, parameters);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("student/{studentId:guid}/summary")]
        [SwaggerOperation(Summary = "Get student fee summary and breakdown")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentFeeSummary(Guid studentId)
        {
            var result = await _feeService.GetStudentFeeSummary(studentId, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,bursar,parent,student")]
        [HttpPost("teller")]
        [SwaggerOperation(Summary = "Post a bank teller or electronic payment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostTeller([FromBody] PostTellerRequest request)
        {
            var result = await _feeService.PostTeller(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,bursar")]
        [HttpPost("teller/{id:guid}/verify")]
        [SwaggerOperation(Summary = "Verify and issue official receipt for fee payment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyTeller(Guid id, [FromBody] VerifyTellerRequest request)
        {
            var result = await _feeService.VerifyTeller(id, request, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("student/{studentId:guid}/receipts")]
        [SwaggerOperation(Summary = "Get receipts issued for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentReceipts(Guid studentId)
        {
            var result = await _feeService.GetStudentReceipts(studentId, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("receipt/{receiptId:guid}/pdf")]
        [SwaggerOperation(Summary = "Get receipt PDF download payload")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReceiptPdf(Guid receiptId)
        {
            var result = await _feeService.GetReceiptPdf(receiptId, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,bursar")]
        [HttpPost("reminders")]
        [SwaggerOperation(Summary = "Queue automated fee payment reminders")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> QueuePaymentReminders([FromBody] QueuePaymentRemindersRequest request)
        {
            var result = await _feeService.QueuePaymentReminders(request);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("bank-accounts")]
        [SwaggerOperation(Summary = "Get active school bank accounts")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBankAccounts()
        {
            var result = await _feeService.GetBankAccounts();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("statement/{studentId:guid}")]
        [SwaggerOperation(Summary = "Get statement of account for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatementOfAccount(Guid studentId)
        {
            var result = await _feeService.GetStatementOfAccount(studentId, HttpContext.User);
            return Ok(result);
        }
    }
}
