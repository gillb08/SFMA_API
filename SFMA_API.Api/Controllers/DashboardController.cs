using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace SFMA_API.Api.Controllers
{
    [Route("api/v1/dashboard")]
    [Authorize(Policy = "Authorization")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("executive", Name = "get-executive-dashboard")]
        [SwaggerOperation(Summary = "Get executive dashboard KPIs")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExecutiveDashboard()
        {
            var result = await _dashboardService.GetExecutiveDashboard();
            return Ok(result);
        }

        [HttpGet("academic-snapshot", Name = "get-academic-snapshot")]
        [SwaggerOperation(Summary = "Get academic snapshot and terminal performance indicators")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAcademicSnapshot()
        {
            var result = await _dashboardService.GetAcademicSnapshot();
            return Ok(result);
        }

        [HttpGet("budget-progress", Name = "get-budget-progress")]
        [SwaggerOperation(Summary = "Get departmental budget vs actual progress")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBudgetProgress()
        {
            var result = await _dashboardService.GetBudgetProgress();
            return Ok(result);
        }

        [HttpGet("financial", Name = "get-financial-dashboard")]
        [SwaggerOperation(Summary = "Get financial performance dashboard")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFinancialDashboard()
        {
            var result = await _dashboardService.GetFinancialDashboard();
            return Ok(result);
        }
    }
}

