using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/students")]
    [Authorize(Policy = "Authorization")]
    public class StudentsController : BaseController
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("", Name = "get-all-students")]
        [SwaggerOperation(Summary = "Get all students with filters and pagination")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllStudents([FromQuery] Guid? classId, [FromQuery] StudentStatus? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _studentService.GetAllStudents(classId, status, parameters, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("{id:guid}", Name = "get-student-by-id")]
        [SwaggerOperation(Summary = "Get student details by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            var result = await _studentService.GetStudentById(id, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("admit", Name = "admit-student")]
        [SwaggerOperation(Summary = "Admit a new student with duplicate detection (409 Conflict if duplicate)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdmitStudent([FromBody] AdmitStudentRequest request)
        {
            var result = await _studentService.AdmitStudent(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}/biodata", Name = "update-student-biodata")]
        [SwaggerOperation(Summary = "Update student bio data")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateBioData(Guid id, [FromBody] UpdateStudentBioDataRequest request)
        {
            var result = await _studentService.UpdateBioData(id, request);
            return Ok(result);
        }

        [HttpGet("{id:guid}/id-card", Name = "get-student-id-card")]
        [SwaggerOperation(Summary = "Get student ID card")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIdCard(Guid id)
        {
            var result = await _studentService.GetIdCard(id, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("batch-print", Name = "batch-print-id-cards")]
        [SwaggerOperation(Summary = "Batch print student ID cards as PDF")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchPrint([FromBody] BatchPrintRequest request)
        {
            var bytes = await _studentService.BatchPrint(request);
            return File(bytes, "application/pdf", "student_id_cards.pdf");
        }
    }
}

