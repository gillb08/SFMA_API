using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/lesson-notes")]
    [Authorize(Policy = "Authorization")]
    public class LessonNotesController : BaseController
    {
        private readonly ILessonNoteAndAssignmentService _service;

        public LessonNotesController(ILessonNoteAndAssignmentService service)
        {
            _service = service;
        }

        [HttpGet("", Name = "get-all-lesson-notes")]
        [SwaggerOperation(Summary = "Get lesson notes with optional status filter")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLessonNotes([FromQuery] LessonNoteStatus? status)
        {
            var result = await _service.GetLessonNotes(status, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("", Name = "create-lesson-note")]
        [SwaggerOperation(Summary = "Submit lesson note for review")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateLessonNote([FromBody] CreateLessonNoteRequest request)
        {
            var result = await _service.CreateLessonNote(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/review", Name = "review-lesson-note")]
        [SwaggerOperation(Summary = "Review/Approve/Request revision on lesson note")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReviewLessonNote(Guid id, [FromBody] ReviewLessonNoteRequest request)
        {
            var result = await _service.ReviewLessonNote(id, request, HttpContext.User);
            return Ok(result);
        }
    }
}

