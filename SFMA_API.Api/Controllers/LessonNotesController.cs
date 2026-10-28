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
        [SwaggerOperation(Summary = "Get lesson notes with optional status, class, or scheme filter")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLessonNotes(
            [FromQuery] LessonNoteStatus? status,
            [FromQuery] Guid? classSectionId,
            [FromQuery] Guid? schemeOfWorkEntryId)
        {
            var result = await _service.GetLessonNotes(status, classSectionId, schemeOfWorkEntryId, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("", Name = "create-lesson-note")]
        [SwaggerOperation(Summary = "Submit lesson note against an approved scheme of work entry")]
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

        [HttpPost("{id:guid}/delivery", Name = "mark-lesson-delivery")]
        [SwaggerOperation(Summary = "Record classroom lesson delivery and progress")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkLessonDelivery(Guid id, [FromBody] MarkLessonDeliveryRequest request)
        {
            var result = await _service.MarkLessonDelivery(id, request, HttpContext.User);
            return Ok(result);
        }
    }
}
