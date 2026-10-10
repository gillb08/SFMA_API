using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        [HttpPost("login", Name = "login")]
        [SwaggerOperation(Summary = "Login user with username/email and password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.Login(request);
            return Ok(result);
        }

        [Authorize(Policy = "Authorization")]
        [HttpPost("logout", Name = "logout")]
        [SwaggerOperation(Summary = "Logout current user")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.Logout(userId);
            return Ok(result);
        }

        [Authorize(Policy = "Authorization")]
        [HttpGet("me", Name = "get-current-user-profile")]
        [SwaggerOperation(Summary = "Get current logged-in user profile")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Me()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.GetCurrentUserProfile(userId);
            return Ok(result);
        }

        [Authorize(Policy = "Authorization")]
        [HttpPut("profile", Name = "update-user-profile")]
        [SwaggerOperation(Summary = "Update current user profile")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.UpdateUserProfile(userId, request);
            return Ok(result);
        }

        [Authorize(Policy = "Authorization")]
        [HttpPost("change-password", Name = "change-password")]
        [SwaggerOperation(Summary = "Change user password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _authService.ChangePassword(userId, request);
            return Ok(result);
        }

        [HttpPost("reset-password", Name = "reset-password")]
        [SwaggerOperation(Summary = "Reset user password via token")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPassword(request);
            return Ok(result);
        }

        [HttpPost("refresh-token", Name = "refresh-token")]
        [SwaggerOperation(Summary = "Refresh expired access token using refresh token")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshAccessTokenRequest request)
        {
            var result = await _authService.RefreshAccessToken(request);
            return Ok(result);
        }
    }
}

