using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using SFMA_API.Models.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthenticationService _authService;
        private readonly EmailOptions _emailOptions;

        public AuthController(IAuthenticationService authService, IOptions<EmailOptions> emailOptions)
        {
            _authService = authService;
            _emailOptions = emailOptions.Value;
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

        [HttpPost("confirm-email", Name = "confirm-email")]
        [SwaggerOperation(Summary = "Confirm user email via verification token")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var result = await _authService.ConfirmEmail(request);
            return Ok(new SuccessResponse { Message = "Email confirmed successfully. You may now proceed to log in." });
        }

        [HttpGet("confirm-email", Name = "confirm-email-get")]
        [SwaggerOperation(Summary = "Confirm email via link and redirect to portal login")]
        public async Task<IActionResult> ConfirmEmailGet([FromQuery] string email, [FromQuery] string token)
        {
            try
            {
                await _authService.ConfirmEmail(new ConfirmEmailRequest { Email = email, Token = token });
                string loginUrl = _emailOptions.PortalLoginUrl;
                string redirectUrl = $"{loginUrl}?verified=true&email={Uri.EscapeDataString(email ?? string.Empty)}";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                string loginUrl = _emailOptions.PortalLoginUrl;
                string redirectUrl = $"{loginUrl}?verified=false&error={Uri.EscapeDataString(ex.Message)}";
                return Redirect(redirectUrl);
            }
        }
    }
}

