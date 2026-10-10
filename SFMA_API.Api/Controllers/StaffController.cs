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
    [Route("api/v1/staff")]
    [Authorize(Policy = "Authorization")]
    public class StaffController : BaseController
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("", Name = "get-all-staff")]
        [SwaggerOperation(Summary = "Get all staff with optional filters and pagination")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllStaff([FromQuery] string? department, [FromQuery] string? roleKey, [FromQuery] string? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _staffService.GetAllStaff(department, roleKey, status, parameters);
            return Ok(result);
        }

        [HttpGet("{id:guid}", Name = "get-staff-by-id")]
        [SwaggerOperation(Summary = "Get staff details by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStaffById(Guid id)
        {
            var result = await _staffService.GetStaffById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("", Name = "create-staff")]
        [SwaggerOperation(Summary = "Create a new staff account")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            var result = await _staffService.CreateStaff(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-staff")]
        [SwaggerOperation(Summary = "Update staff profile")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffRequest request)
        {
            var result = await _staffService.UpdateStaff(id, request);
            return Ok(result);
        }

        [HttpPatch("{id:guid}/status", Name = "update-staff-status")]
        [SwaggerOperation(Summary = "Update staff active status")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStaffStatus(Guid id, [FromBody] UpdateStaffStatusRequest request)
        {
            var result = await _staffService.UpdateStaffStatus(id, request);
            return Ok(result);
        }

        [HttpPost("{id:guid}/reset-password", Name = "admin-reset-password")]
        [SwaggerOperation(Summary = "Administrative reset of staff password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminResetPassword(Guid id)
        {
            var newPassword = await _staffService.AdminResetPassword(id);
            return Ok(new { TemporaryPassword = newPassword });
        }
    }
}

