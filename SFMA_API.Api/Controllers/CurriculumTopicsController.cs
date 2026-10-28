using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/curriculum-topics")]
    [Authorize(Policy = "Authorization")]
    public class CurriculumTopicsController : BaseController
    {
        private readonly ICurriculumTopicService _service;

        public CurriculumTopicsController(ICurriculumTopicService service)
        {
            _service = service;
        }

        [HttpGet("", Name = "get-curriculum-topics")]
        [SwaggerOperation(Summary = "Get curriculum topics by subject, class section, or academic term")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurriculumTopics(
            [FromQuery] Guid? subjectId,
            [FromQuery] Guid? classSectionId,
            [FromQuery] Guid? academicTermId)
        {
            var result = await _service.GetCurriculumTopics(subjectId, classSectionId, academicTermId, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("", Name = "create-curriculum-topic")]
        [SwaggerOperation(Summary = "Create a new curriculum topic and expected learning outcomes")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateCurriculumTopic([FromBody] CreateCurriculumTopicRequest request)
        {
            var result = await _service.CreateCurriculumTopic(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-curriculum-topic")]
        [SwaggerOperation(Summary = "Update curriculum topic details or outcomes")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateCurriculumTopic(Guid id, [FromBody] UpdateCurriculumTopicRequest request)
        {
            var result = await _service.UpdateCurriculumTopic(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpDelete("{id:guid}", Name = "delete-curriculum-topic")]
        [SwaggerOperation(Summary = "Soft delete / deactivate a curriculum topic")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteCurriculumTopic(Guid id)
        {
            var result = await _service.DeleteCurriculumTopic(id, HttpContext.User);
            return Ok(result);
        }
    }
}
