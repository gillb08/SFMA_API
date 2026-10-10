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
    [Route("api/v1/assessments")]
    [Authorize(Policy = "Authorization")]
    public class AssessmentsController : BaseController
    {
        private readonly IAssessmentService _assessmentService;

        public AssessmentsController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        [HttpGet("broadsheet", Name = "get-assessment-broadsheet")]
        [SwaggerOperation(Summary = "Get assessment broadsheet for class and subject")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBroadsheet([FromQuery] Guid classId, [FromQuery] string subject, [FromQuery] Guid termId)
        {
            var result = await _assessmentService.GetBroadsheet(classId, subject, termId, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("batch", Name = "batch-update-scores")]
        [SwaggerOperation(Summary = "Batch enter CA and Exam scores (Enforces 403 Forbidden on deadline elapsed)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateScores([FromBody] BatchUpdateAssessmentsRequest request)
        {
            var result = await _assessmentService.BatchUpdateScores(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("submit", Name = "submit-broadsheet")]
        [SwaggerOperation(Summary = "Submit broadsheet for approval")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitBroadsheet([FromBody] BroadsheetActionRequest request)
        {
            var result = await _assessmentService.SubmitBroadsheet(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("approve", Name = "approve-broadsheet")]
        [SwaggerOperation(Summary = "Approve and publish broadsheet")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveBroadsheet([FromBody] BroadsheetActionRequest request)
        {
            var result = await _assessmentService.ApproveBroadsheet(request, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("student/{studentId:guid}", Name = "get-student-results")]
        [SwaggerOperation(Summary = "Get terminal assessment results for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentResults(Guid studentId, [FromQuery] Guid? termId)
        {
            var result = await _assessmentService.GetStudentResults(studentId, termId, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("export", Name = "export-broadsheet-results")]
        [SwaggerOperation(Summary = "Export broadsheet results as CSV or PDF")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportResults([FromQuery] Guid classId, [FromQuery] string subject, [FromQuery] Guid termId, [FromQuery] string format = "csv")
        {
            var content = await _assessmentService.ExportResults(classId, subject, termId, format);
            var bytes = Encoding.UTF8.GetBytes(content);
            return File(bytes, "text/csv", "broadsheet_results.csv");
        }

        [HttpPost("import-csv", Name = "import-scores-csv")]
        [SwaggerOperation(Summary = "Import assessment scores via CSV string payload")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportScoresCsv([FromQuery] Guid classId, [FromQuery] string subject, [FromQuery] Guid termId, [FromBody] string csvContent)
        {
            var result = await _assessmentService.ImportScoresCsv(classId, subject, termId, csvContent, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("template", Name = "get-score-template-csv")]
        [SwaggerOperation(Summary = "Download CSV template for scores upload")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetScoreTemplateCsv()
        {
            var template = await _assessmentService.GetScoreTemplateCsv();
            var bytes = Encoding.UTF8.GetBytes(template);
            return File(bytes, "text/csv", "score_import_template.csv");
        }
    }
}

