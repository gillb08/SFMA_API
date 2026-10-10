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
    [Route("api/v1/requirements")]
    [Authorize(Policy = "Authorization")]
    public class RequirementsController : BaseController
    {
        private readonly IInquiryAndRequirementService _service;

        public RequirementsController(IInquiryAndRequirementService service)
        {
            _service = service;
        }

        [HttpGet("", Name = "get-school-requirements")]
        [SwaggerOperation(Summary = "Get student school requirements and textbook list")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolRequirements([FromQuery] Guid classSectionId, [FromQuery] Guid? termId, [FromQuery] Guid? studentId)
        {
            var result = await _service.GetSchoolRequirements(classSectionId, termId, studentId);
            return Ok(result);
        }

        [HttpPost("{id:guid}/status", Name = "toggle-requirement-status")]
        [SwaggerOperation(Summary = "Toggle acquired status of requirement item for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToggleRequirementStatus(Guid id, [FromBody] ToggleRequirementStatusRequest request)
        {
            var result = await _service.ToggleRequirementStatus(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("export-pdf", Name = "export-requirements-pdf")]
        [SwaggerOperation(Summary = "Export requirements list as PDF base64")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportRequirementsPdf([FromQuery] Guid classSectionId)
        {
            var base64Pdf = await _service.ExportRequirementsPdf(classSectionId);
            return Ok(new { Base64Pdf = base64Pdf });
        }
    }
}

