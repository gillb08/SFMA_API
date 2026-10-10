using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/terms")]
    [Authorize(Policy = "Authorization")]
    public class TermsController : BaseController
    {
        private readonly ITermService _termService;

        public TermsController(ITermService termService)
        {
            _termService = termService;
        }

        [HttpGet("", Name = "get-all-terms")]
        [SwaggerOperation(Summary = "Get all academic terms")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTerms()
        {
            var result = await _termService.GetAllTerms();
            return Ok(result);
        }

        [HttpGet("active", Name = "get-active-term")]
        [SwaggerOperation(Summary = "Get currently active academic term")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveTerm()
        {
            var result = await _termService.GetActiveTerm();
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-term")]
        [SwaggerOperation(Summary = "Update term dates and status")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTerm(Guid id, [FromBody] UpdateTermRequest request)
        {
            var result = await _termService.UpdateTerm(id, request);
            return Ok(result);
        }

        [HttpPost("{id:guid}/deadlines", Name = "configure-deadlines")]
        [SwaggerOperation(Summary = "Configure assessment and gradebook submission deadlines")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfigureDeadlines(Guid id, [FromBody] ConfigureDeadlinesRequest request)
        {
            var result = await _termService.ConfigureDeadlines(id, request);
            return Ok(result);
        }
    }
}

