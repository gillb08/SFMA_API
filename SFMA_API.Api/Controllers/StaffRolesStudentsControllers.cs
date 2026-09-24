using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/staff")]
    [Authorize(Policy = "Authorization")]
    public class StaffController : BaseController
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("", Name = "get-all-staff")]
        [SwaggerOperation(Summary = "Get all staff with optional filters and pagination")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllStaff([FromQuery] string? department, [FromQuery] string? roleKey, [FromQuery] string? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _staffService.GetAllStaff(department, roleKey, status, parameters);
            return Ok(result);
        }

        [HttpGet("{id:guid}", Name = "get-staff-by-id")]
        [SwaggerOperation(Summary = "Get staff details by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStaffById(Guid id)
        {
            var result = await _staffService.GetStaffById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("", Name = "create-staff")]
        [SwaggerOperation(Summary = "Create a new staff account")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            var result = await _staffService.CreateStaff(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}", Name = "update-staff")]
        [SwaggerOperation(Summary = "Update staff profile")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffRequest request)
        {
            var result = await _staffService.UpdateStaff(id, request);
            return Ok(result);
        }

        [HttpPatch("{id:guid}/status", Name = "update-staff-status")]
        [SwaggerOperation(Summary = "Update staff active status")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStaffStatus(Guid id, [FromBody] UpdateStaffStatusRequest request)
        {
            var result = await _staffService.UpdateStaffStatus(id, request);
            return Ok(result);
        }

        [HttpPost("{id:guid}/reset-password", Name = "admin-reset-password")]
        [SwaggerOperation(Summary = "Administrative reset of staff password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminResetPassword(Guid id)
        {
            var newPassword = await _staffService.AdminResetPassword(id);
            return Ok(new { TemporaryPassword = newPassword });
        }
    }

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
