using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/assignments")]
    [Authorize(Policy = "Authorization")]
    public class AssignmentsController : BaseController
    {
        private readonly ILessonNoteAndAssignmentService _service;

        public AssignmentsController(ILessonNoteAndAssignmentService service)
        {
            _service = service;
        }

        [HttpGet("", Name = "get-assignments")]
        [SwaggerOperation(Summary = "Get assignments for class")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignments([FromQuery] Guid classSectionId)
        {
            var result = await _service.GetAssignments(classSectionId, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("", Name = "create-assignment")]
        [SwaggerOperation(Summary = "Create a homework or project assignment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
        {
            var result = await _service.CreateAssignment(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-assignment")]
        [SwaggerOperation(Summary = "Update assignment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] UpdateAssignmentRequest request)
        {
            var result = await _service.UpdateAssignment(id, request);
            return Ok(result);
        }

        [HttpGet("{id:guid}/submissions", Name = "get-assignment-submissions")]
        [SwaggerOperation(Summary = "Get student submissions for assignment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid id)
        {
            var result = await _service.GetAssignmentSubmissions(id, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/submit", Name = "submit-assignment")]
        [SwaggerOperation(Summary = "Submit assignment response")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitAssignment(Guid id, [FromBody] SubmitAssignmentRequest request)
        {
            var result = await _service.SubmitAssignment(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpPut("submissions/{submissionId:guid}/grade", Name = "grade-assignment-submission")]
        [SwaggerOperation(Summary = "Grade a student assignment submission")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeAssignmentSubmissionRequest request)
        {
            var result = await _service.GradeAssignmentSubmission(submissionId, request, HttpContext.User);
            return Ok(result);
        }
    }
}

