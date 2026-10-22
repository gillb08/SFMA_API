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
    [Route("api/v1/requisitions")]
    [Authorize(Policy = "Authorization")]
    public class RequisitionsController : BaseController
    {
        private readonly IRequisitionService _requisitionService;

        public RequisitionsController(IRequisitionService requisitionService)
        {
            _requisitionService = requisitionService;
        }

        [HttpGet("", Name = "get-all-requisitions")]
        [SwaggerOperation(Summary = "Get requisitions with filters")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequisitions([FromQuery] RequisitionStatus? status, [FromQuery] string? department, [FromQuery] RequestParameters parameters)
        {
            var result = await _requisitionService.GetRequisitions(status, department, parameters, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("{id:guid}", Name = "get-requisition-by-id")]
        [SwaggerOperation(Summary = "Get requisition details by ID")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequisitionById(Guid id)
        {
            var result = await _requisitionService.GetRequisitionById(id, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("", Name = "create-requisition")]
        [SwaggerOperation(Summary = "Create a new requisition")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionRequest request)
        {
            var result = await _requisitionService.CreateRequisition(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/approve", Name = "approve-requisition")]
        [SwaggerOperation(Summary = "Advance requisition approval stage (AcademicHead -> FinancialHead -> SuperAdmin)")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveRequisition(Guid id, [FromBody] ApproveRequisitionRequest request)
        {
            var result = await _requisitionService.ApproveRequisition(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("{id:guid}/query", Name = "query-requisition")]
        [SwaggerOperation(Summary = "Query / reject requisition")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> QueryRequisition(Guid id, [FromBody] QueryRequisitionRequest request)
        {
            var result = await _requisitionService.QueryRequisition(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("budgets", Name = "upsert-department-budget")]
        [SwaggerOperation(Summary = "Set or update approved term budget allocation for a department")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertDepartmentBudget([FromBody] UpsertDepartmentBudgetRequest request)
        {
            var result = await _requisitionService.UpsertDepartmentBudget(request);
            return Ok(result);
        }
    }
}

