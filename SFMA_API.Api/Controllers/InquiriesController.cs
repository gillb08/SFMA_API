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
    [Route("api/v1/inquiries")]
    [Authorize(Policy = "Authorization")]
    public class InquiriesController : BaseController
    {
        private readonly IInquiryAndRequirementService _service;

        public InquiriesController(IInquiryAndRequirementService service)
        {
            _service = service;
        }

        [HttpGet("", Name = "get-inquiries")]
        [SwaggerOperation(Summary = "Get helpdesk inquiries")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInquiries([FromQuery] InquiryStatus? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _service.GetInquiries(status, parameters, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("{id:guid}", Name = "get-inquiry-by-id")]
        [SwaggerOperation(Summary = "Get inquiry thread by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInquiryById(Guid id)
        {
            var result = await _service.GetInquiryById(id, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("", Name = "create-inquiry")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Create an inquiry or support request")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateInquiry([FromBody] CreateInquiryRequest request)
        {
            var result = await _service.CreateInquiry(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/messages", Name = "add-inquiry-message")]
        [SwaggerOperation(Summary = "Post message to inquiry thread")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddInquiryMessage(Guid id, [FromBody] AddInquiryMessageRequest request)
        {
            var result = await _service.AddInquiryMessage(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpPatch("{id:guid}/status", Name = "update-inquiry-status")]
        [SwaggerOperation(Summary = "Update inquiry status")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateInquiryStatus(Guid id, [FromBody] UpdateInquiryStatusRequest request)
        {
            var result = await _service.UpdateInquiryStatus(id, request, HttpContext.User);
            return Ok(result);
        }
    }
}

