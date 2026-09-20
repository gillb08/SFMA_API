using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [SwaggerResponse(StatusCodes.Status403Forbidden, Description = "User is forbidden", Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, Description = "User is unauthorized", Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, Description = "Internal server error", Type = typeof(ErrorResponse))]
    [Produces("application/json")]
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected readonly UserManager<ApplicationUser>? _userManager;

        protected BaseController()
        {
        }

        protected BaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [NonAction]
        public override OkObjectResult Ok(object? value)
        {
            SuccessResponse response = new()
            {
                Success = true
            };
            if (value is string str)
                response.Message = str;
            else
                response.Data = value;

            return base.Ok(response);
        }

        [NonAction]
        public new OkObjectResult Ok()
        {
            SuccessResponse response = new()
            {
                Success = true
            };

            return base.Ok(response);
        }

        [NonAction]
        public new NotFoundObjectResult NotFound()
        {
            SuccessResponse response = new()
            {
                Success = false,
                Message = "Resource not found"
            };

            return base.NotFound(response);
        }

        [NonAction]
        public OkObjectResult Invalid(string message = "Invalid request")
        {
            SuccessResponse response = new()
            {
                Success = false,
                Message = message
            };

            return base.Ok(response);
        }

        [NonAction]
        public async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            if (_userManager == null) return null;
            return await _userManager.GetUserAsync(HttpContext.User);
        }

        [NonAction]
        public async Task<string?> GetUserIdAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Id;
        }

        [NonAction]
        public string? GetUserId()
        {
            // "Id" is our canonical JWT claim. NameIdentifier is retained for
            // interoperability with ASP.NET Identity and older issued tokens.
            return HttpContext.User.FindFirstValue("Id")
                ?? HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
