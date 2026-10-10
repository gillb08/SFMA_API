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
    [Route("api/v1/timetable")]
    [Authorize(Policy = "Authorization")]
    public class TimetableController : BaseController
    {
        private readonly ISchemeAndTimetableService _service;

        public TimetableController(ISchemeAndTimetableService service)
        {
            _service = service;
        }

        [HttpGet("{classId:guid}", Name = "get-timetable")]
        [SwaggerOperation(Summary = "Get class timetable slots")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTimetable(Guid classId)
        {
            var result = await _service.GetTimetable(classId);
            return Ok(result);
        }

        [HttpPut("{classId:guid}", Name = "update-timetable")]
        [SwaggerOperation(Summary = "Update class timetable slots")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTimetable(Guid classId, [FromBody] UpdateTimetableRequest request)
        {
            var result = await _service.UpdateTimetable(classId, request);
            return Ok(result);
        }
    }
}

