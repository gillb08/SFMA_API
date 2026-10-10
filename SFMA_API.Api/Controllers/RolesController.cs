using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/roles")]
    [Authorize(Policy = "Authorization")]
    public class RolesController : BaseController
    {
        private readonly IRolePermissionService _roleService;

        public RolesController(IRolePermissionService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet("", Name = "get-all-roles")]
        [SwaggerOperation(Summary = "Get all configured portal roles")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _roleService.GetAllRoles();
            return Ok(result);
        }

        [HttpGet("{roleKey}/permissions", Name = "get-role-permissions")]
        [SwaggerOperation(Summary = "Get permissions for specific role")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRolePermissions(string roleKey)
        {
            var result = await _roleService.GetRolePermissions(roleKey);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}

