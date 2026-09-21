using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Models.Enums;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/requisitions")]
    public class RequisitionsController : BaseController
    {
        private readonly IRequisitionService _requisitionService;

        public RequisitionsController(IRequisitionService requisitionService)
        {
            _requisitionService = requisitionService;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get requisitions with filters")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequisitions([FromQuery] RequisitionStatus? status, [FromQuery] string? department, [FromQuery] RequestParameters parameters)
        {
            var result = await _requisitionService.GetRequisitions(status, department, parameters, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "Get requisition details by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequisitionById(Guid id)
        {
            var result = await _requisitionService.GetRequisitionById(id, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,financial_admin,teacher")]
        [HttpPost]
        [SwaggerOperation(Summary = "Create a new requisition")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionRequest request)
        {
            var result = await _requisitionService.CreateRequisition(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,financial_admin")]
        [HttpPost("{id:guid}/approve")]
        [SwaggerOperation(Summary = "Advance requisition approval stage (AcademicHead -> FinancialHead -> SuperAdmin)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveRequisition(Guid id, [FromBody] ApproveRequisitionRequest request)
        {
            var result = await _requisitionService.ApproveRequisition(id, request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,financial_admin")]
        [HttpPost("{id:guid}/query")]
        [SwaggerOperation(Summary = "Query / reject requisition")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> QueryRequisition(Guid id, [FromBody] QueryRequisitionRequest request)
        {
            var result = await _requisitionService.QueryRequisition(id, request, HttpContext.User);
            return Ok(result);
        }
    }

    [Route("api/v1/scheme-of-work")]
    public class SchemeOfWorkController : BaseController
    {
        private readonly ISchemeAndTimetableService _service;

        public SchemeOfWorkController(ISchemeAndTimetableService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get weekly scheme of work curriculum")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchemeOfWork([FromQuery] Guid classSectionId, [FromQuery] Guid? termId)
        {
            var result = await _service.GetSchemeOfWork(classSectionId, termId);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,teacher")]
        [HttpPost]
        [SwaggerOperation(Summary = "Add scheme of work week")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSchemeOfWork([FromBody] CreateSchemeOfWorkRequest request)
        {
            var result = await _service.CreateSchemeOfWork(request);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,teacher")]
        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "Update scheme of work entry")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSchemeOfWork(Guid id, [FromBody] UpdateSchemeOfWorkRequest request)
        {
            var result = await _service.UpdateSchemeOfWork(id, request);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head")]
        [HttpPost("{id:guid}/vet")]
        [SwaggerOperation(Summary = "Vet scheme of work entry by Head of School")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> VetSchemeOfWork(Guid id)
        {
            var result = await _service.VetSchemeOfWork(id, HttpContext.User);
            return Ok(result);
        }
    }

    [Route("api/v1/timetable")]
    public class TimetableController : BaseController
    {
        private readonly ISchemeAndTimetableService _service;

        public TimetableController(ISchemeAndTimetableService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("{classId:guid}")]
        [SwaggerOperation(Summary = "Get class timetable slots")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTimetable(Guid classId)
        {
            var result = await _service.GetTimetable(classId);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head")]
        [HttpPut("{classId:guid}")]
        [SwaggerOperation(Summary = "Update class timetable slots")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTimetable(Guid classId, [FromBody] UpdateTimetableRequest request)
        {
            var result = await _service.UpdateTimetable(classId, request);
            return Ok(result);
        }
    }

    [Route("api/v1/lesson-notes")]
    public class LessonNotesController : BaseController
    {
        private readonly ILessonNoteAndAssignmentService _service;

        public LessonNotesController(ILessonNoteAndAssignmentService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get lesson notes with optional status filter")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLessonNotes([FromQuery] LessonNoteStatus? status)
        {
            var result = await _service.GetLessonNotes(status, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,teacher")]
        [HttpPost]
        [SwaggerOperation(Summary = "Submit lesson note for review")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateLessonNote([FromBody] CreateLessonNoteRequest request)
        {
            var result = await _service.CreateLessonNote(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head")]
        [HttpPost("{id:guid}/review")]
        [SwaggerOperation(Summary = "Review/Approve/Request revision on lesson note")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReviewLessonNote(Guid id, [FromBody] ReviewLessonNoteRequest request)
        {
            var result = await _service.ReviewLessonNote(id, request, HttpContext.User);
            return Ok(result);
        }
    }

    [Route("api/v1/assignments")]
    public class AssignmentsController : BaseController
    {
        private readonly ILessonNoteAndAssignmentService _service;

        public AssignmentsController(ILessonNoteAndAssignmentService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get assignments for class")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignments([FromQuery] Guid classSectionId)
        {
            var result = await _service.GetAssignments(classSectionId, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,teacher")]
        [HttpPost]
        [SwaggerOperation(Summary = "Create a homework or project assignment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
        {
            var result = await _service.CreateAssignment(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,teacher")]
        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "Update assignment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] UpdateAssignmentRequest request)
        {
            var result = await _service.UpdateAssignment(id, request);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id:guid}/submissions")]
        [SwaggerOperation(Summary = "Get student submissions for assignment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid id)
        {
            var result = await _service.GetAssignmentSubmissions(id, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,student")]
        [HttpPost("{id:guid}/submit")]
        [SwaggerOperation(Summary = "Submit assignment response")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitAssignment(Guid id, [FromBody] SubmitAssignmentRequest request)
        {
            var result = await _service.SubmitAssignment(id, request, HttpContext.User);
            return Ok(result);
        }
    }

    [Route("api/v1/inquiries")]
    public class InquiriesController : BaseController
    {
        private readonly IInquiryAndRequirementService _service;

        public InquiriesController(IInquiryAndRequirementService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get helpdesk inquiries")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInquiries([FromQuery] InquiryStatus? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _service.GetInquiries(status, parameters, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "Get inquiry thread by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInquiryById(Guid id)
        {
            var result = await _service.GetInquiryById(id, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [SwaggerOperation(Summary = "Create an inquiry or support request")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateInquiry([FromBody] CreateInquiryRequest request)
        {
            var result = await _service.CreateInquiry(request, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id:guid}/messages")]
        [SwaggerOperation(Summary = "Post message to inquiry thread")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddInquiryMessage(Guid id, [FromBody] AddInquiryMessageRequest request)
        {
            var result = await _service.AddInquiryMessage(id, request, HttpContext.User);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,financial_admin")]
        [HttpPatch("{id:guid}/status")]
        [SwaggerOperation(Summary = "Update inquiry status")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateInquiryStatus(Guid id, [FromBody] UpdateInquiryStatusRequest request)
        {
            var result = await _service.UpdateInquiryStatus(id, request, HttpContext.User);
            return Ok(result);
        }
    }

    [Route("api/v1/requirements")]
    public class RequirementsController : BaseController
    {
        private readonly IInquiryAndRequirementService _service;

        public RequirementsController(IInquiryAndRequirementService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Get student school requirements and textbook list")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolRequirements([FromQuery] Guid classSectionId, [FromQuery] Guid? termId, [FromQuery] Guid? studentId)
        {
            var result = await _service.GetSchoolRequirements(classSectionId, termId, studentId);
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,teacher")]
        [HttpPost("{id:guid}/status")]
        [SwaggerOperation(Summary = "Toggle acquired status of requirement item for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToggleRequirementStatus(Guid id, [FromBody] ToggleRequirementStatusRequest request)
        {
            var result = await _service.ToggleRequirementStatus(id, request, HttpContext.User);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("export-pdf")]
        [SwaggerOperation(Summary = "Export requirements list as PDF base64")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportRequirementsPdf([FromQuery] Guid classSectionId)
        {
            var base64Pdf = await _service.ExportRequirementsPdf(classSectionId);
            return Ok(new { Base64Pdf = base64Pdf });
        }
    }

    [Route("api/v1/dashboard")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head,financial_admin")]
        [HttpGet("executive")]
        [SwaggerOperation(Summary = "Get executive dashboard KPIs")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExecutiveDashboard()
        {
            var result = await _dashboardService.GetExecutiveDashboard();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,academic_head")]
        [HttpGet("academic-snapshot")]
        [SwaggerOperation(Summary = "Get academic snapshot and terminal performance indicators")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAcademicSnapshot()
        {
            var result = await _dashboardService.GetAcademicSnapshot();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,financial_admin")]
        [HttpGet("budget-progress")]
        [SwaggerOperation(Summary = "Get departmental budget vs actual progress")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBudgetProgress()
        {
            var result = await _dashboardService.GetBudgetProgress();
            return Ok(result);
        }

        [Authorize(Roles = "super_admin,academic_admin,financial_admin")]
        [HttpGet("financial")]
        [SwaggerOperation(Summary = "Get financial performance dashboard")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFinancialDashboard()
        {
            var result = await _dashboardService.GetFinancialDashboard();
            return Ok(result);
        }
    }
}
