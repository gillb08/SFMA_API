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
    [Route("api/v1/scheme-of-work")]
    [Authorize(Policy = "Authorization")]
    public class SchemeOfWorkController : BaseController
    {
        private readonly ISchemeAndTimetableService _service;

        public SchemeOfWorkController(ISchemeAndTimetableService service)
        {
            _service = service;
        }

        [HttpGet("", Name = "get-scheme-of-work")]
        [SwaggerOperation(Summary = "Get weekly scheme of work curriculum")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchemeOfWork(
            [FromQuery] Guid classSectionId,
            [FromQuery] Guid? termId,
            [FromQuery] Guid? subjectId)
        {
            var result = await _service.GetSchemeOfWork(classSectionId, termId, subjectId, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("", Name = "create-scheme-of-work")]
        [SwaggerOperation(Summary = "Add scheme of work week")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSchemeOfWork([FromBody] CreateSchemeOfWorkRequest request)
        {
            var result = await _service.CreateSchemeOfWork(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-scheme-of-work")]
        [SwaggerOperation(Summary = "Update scheme of work entry")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSchemeOfWork(Guid id, [FromBody] UpdateSchemeOfWorkRequest request)
        {
            var result = await _service.UpdateSchemeOfWork(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/review", Name = "review-scheme-of-work")]
        [SwaggerOperation(Summary = "Review/Approve/Request revision on scheme of work entry")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReviewSchemeOfWork(Guid id, [FromBody] ReviewSchemeOfWorkRequest request)
        {
            var result = await _service.ReviewSchemeOfWork(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/vet", Name = "vet-scheme-of-work")]
        [SwaggerOperation(Summary = "Vet scheme of work entry by Head of School (compatibility endpoint)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> VetSchemeOfWork(Guid id)
        {
            var result = await _service.VetSchemeOfWork(id, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("coverage-report", Name = "get-curriculum-coverage-report")]
        [SwaggerOperation(Summary = "Get curriculum and scheme coverage report")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCoverageReport([FromQuery] Guid? classSectionId, [FromQuery] Guid? termId)
        {
            var result = await _service.GetCoverageReport(classSectionId, termId, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("alerts", Name = "get-curriculum-alerts")]
        [SwaggerOperation(Summary = "Get curriculum, scheme, and lesson note workflow alerts")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurriculumAlerts([FromQuery] Guid? classSectionId)
        {
            var result = await _service.GetCurriculumAlerts(classSectionId, HttpContext.User);
            return Ok(result);
        }
    }
}
