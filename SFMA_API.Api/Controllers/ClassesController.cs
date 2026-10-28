using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Implementation;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/classes")]
    [Authorize(Policy = "Authorization")]
    public class ClassesController : BaseController
    {
        private readonly IClassSectionService _classSectionService;

        public ClassesController(IClassSectionService classSectionService)
        {
            _classSectionService = classSectionService;
        }

        [HttpGet("", Name = "get-all-classes")]
        [SwaggerOperation(Summary = "Get all class sections, optionally filtered by academic term")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllClasses([FromQuery] Guid? termId = null)
        {
            var result = await _classSectionService.GetAllClasses(termId);
            return Ok(result);
        }

        [HttpGet("{id:guid}", Name = "get-class-by-id")]
        [SwaggerOperation(Summary = "Get a class section by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClassById(Guid id)
        {
            var result = await _classSectionService.GetClassById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("", Name = "create-class")]
        [SwaggerOperation(Summary = "Create a new class section")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassSectionRequest request)
        {
            ClassSectionHelper.ValidateAdministrativeMutationAccess(HttpContext.User, "create-class", "class sections");
            var result = await _classSectionService.CreateClass(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-class")]
        [SwaggerOperation(Summary = "Update a class section")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassSectionRequest request)
        {
            ClassSectionHelper.ValidateAdministrativeMutationAccess(HttpContext.User, "update-class", "class sections");
            var result = await _classSectionService.UpdateClass(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}", Name = "delete-class")]
        [SwaggerOperation(Summary = "Delete a class section")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            ClassSectionHelper.ValidateAdministrativeMutationAccess(HttpContext.User, "delete-class", "class sections");
            var result = await _classSectionService.DeleteClass(id);
            return Ok(result);
        }

        [HttpGet("divisions", Name = "get-all-divisions")]
        [SwaggerOperation(Summary = "Get all distinct class division names in the school")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDivisions()
        {
            var result = await _classSectionService.GetDivisions();
            return Ok(result);
        }
    }
}
