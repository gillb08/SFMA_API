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
    [Route("api/v1/menus")]
    [Route("api/Menus")]
    [Authorize(Policy = "Authorization")]
    public class MenusController : BaseController
    {
        private readonly IMenuService _menuService;

        public MenusController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet("", Name = "get-all-menus")]
        [SwaggerOperation(Summary = "Get all configured UI menus with associated claims")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllMenus()
        {
            var result = await _menuService.GetAllMenus();
            return Ok(result);
        }

        [HttpPost("create-menu", Name = "create-menu")]
        [SwaggerOperation(Summary = "Create a new UI menu group")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMenu([FromBody] CreateMenuRequest request)
        {
            var result = await _menuService.CreateMenu(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-menu")]
        [SwaggerOperation(Summary = "Update existing UI menu details and claims")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMenu(Guid id, [FromBody] UpdateMenuRequest request)
        {
            var result = await _menuService.UpdateMenu(id, request);
            return Ok(result);
        }

        [HttpPost("add-claims-to-menu", Name = "add-claims-to-menu")]
        [SwaggerOperation(Summary = "Associate route permission claims to a menu group")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddClaimsToMenu([FromBody] AddClaimsToMenuRequest request)
        {
            var result = await _menuService.AddClaimsToMenu(request);
            return Ok(result);
        }
    }
}
