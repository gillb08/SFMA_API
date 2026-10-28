using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Implementation;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/subjects")]
    [Authorize(Policy = "Authorization")]
    public class SubjectsController : BaseController
    {
        private readonly ISubjectService _subjectService;

        public SubjectsController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [HttpGet("", Name = "get-all-subjects")]
        [SwaggerOperation(Summary = "Get all school curriculum subjects, optionally filtered by division, department, class section, or academic term")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSubjects(
            [FromQuery] string? division = null,
            [FromQuery] string? department = null,
            [FromQuery] Guid? classSectionId = null,
            [FromQuery] Guid? academicTermId = null,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _subjectService.GetAllSubjects(division, department, classSectionId, academicTermId, activeOnly);
            return Ok(result);
        }

        [HttpPost("", Name = "create-subject")]
        [SwaggerOperation(Summary = "Create a new school curriculum subject")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(HttpContext.User, "create-subject");
            var result = await _subjectService.CreateSubject(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-subject")]
        [SwaggerOperation(Summary = "Update a curriculum subject")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpdateSubjectRequest request)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(HttpContext.User, "update-subject");
            var result = await _subjectService.UpdateSubject(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpDelete("{id:guid}", Name = "delete-subject")]
        [SwaggerOperation(Summary = "Delete a curriculum subject")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteSubject(Guid id)
        {
            ClassSectionHelper.ValidateCurriculumMutationAccess(HttpContext.User, "delete-subject");
            var result = await _subjectService.DeleteSubject(id, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("config", Name = "get-school-config")]
        [SwaggerOperation(Summary = "Get persistent school operational configuration, departments, divisions, and inquiry recipients")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolConfig()
        {
            var result = await _subjectService.GetSchoolConfig(HttpContext.User);
            return Ok(result);
        }
    }
}
