using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/role-claims")]
    [Authorize(Policy = "Authorization")]
    public class RoleClaimsController : BaseController
    {
        private readonly IRoleClaimService _roleClaimService;

        public RoleClaimsController(IRoleClaimService roleClaimService)
        {
            _roleClaimService = roleClaimService;
        }

        [HttpGet("permissions", Name = "get-all-permissions")]
        [SwaggerOperation(Summary = "Get all dynamic route permission tokens discovered in application")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPermissions()
        {
            var result = await _roleClaimService.GetRouteNames();
            return Ok(result);
        }

        [HttpGet("roles", Name = "get-all-role-claims")]
        [SwaggerOperation(Summary = "Get all roles with their assigned claims")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _roleClaimService.GetAllRoles();
            return Ok(result);
        }

        [HttpGet("role/{roleKey}", Name = "get-role-claims-by-key")]
        [SwaggerOperation(Summary = "Get claims and permissions assigned to a specific role")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRolePermissions(string roleKey)
        {
            var result = await _roleClaimService.GetRolePermissions(roleKey);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("update-role-claims", Name = "update-role-claims")]
        [SwaggerOperation(Summary = "Assign or update route permissions for a role")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateRoleClaims([FromBody] UpdateRoleClaimsRequest request)
        {
            var result = await _roleClaimService.UpdateRoleClaims(request);
            return Ok(result);
        }

        [HttpPost("update-user-claims", Name = "update-user-claims")]
        [SwaggerOperation(Summary = "Assign or update custom route permissions for a specific user")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUserClaims([FromBody] UpdateUserClaimsRequest request)
        {
            var result = await _roleClaimService.UpdateUserClaims(request);
            return Ok(result);
        }
    }
}
