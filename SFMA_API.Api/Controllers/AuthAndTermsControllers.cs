using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login user with username/email and password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.Login(request);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        [SwaggerOperation(Summary = "Logout current user")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.Logout(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        [SwaggerOperation(Summary = "Get current logged-in user profile")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Me()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.GetCurrentUserProfile(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        [SwaggerOperation(Summary = "Change user password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.ChangePassword(userId, request);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Reset user password via token")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPassword(request);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        [SwaggerOperation(Summary = "Refresh expired access token using refresh token")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshAccessTokenRequest request)
        {
            var result = await _authService.RefreshAccessToken(request);
            return Ok(result);
        }
    }

    [Route("api/v1/terms")]
    public class TermsController : BaseController
    {
        private readonly ITermService _termService;

        public TermsController(ITermService termService)
        {
            _termService = termService;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get all academic terms")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTerms()
        {
            var result = await _termService.GetAllTerms();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("active")]
        [SwaggerOperation(Summary = "Get currently active academic term")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveTerm()
        {
            var result = await _termService.GetActiveTerm();
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal")]
        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "Update term dates and status")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTerm(Guid id, [FromBody] UpdateTermRequest request)
        {
            var result = await _termService.UpdateTerm(id, request);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,principal,vice_principal_acad")]
        [HttpPost("{id:guid}/deadlines")]
        [SwaggerOperation(Summary = "Configure assessment and gradebook submission deadlines")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfigureDeadlines(Guid id, [FromBody] ConfigureDeadlinesRequest request)
        {
            var result = await _termService.ConfigureDeadlines(id, request);
            return Ok(result);
        }
    }
}
