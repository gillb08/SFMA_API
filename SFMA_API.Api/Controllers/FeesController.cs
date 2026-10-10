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
    [Route("api/v1/fees")]
    [Authorize(Policy = "Authorization")]
    public class FeesController : BaseController
    {
        private readonly IFeeService _feeService;

        public FeesController(IFeeService feeService)
        {
            _feeService = feeService;
        }

        [HttpGet("schedule", Name = "get-fee-schedule")]
        [SwaggerOperation(Summary = "Get fee schedule for term and class")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeeSchedule([FromQuery] Guid termId, [FromQuery] Guid? classSectionId)
        {
            var result = await _feeService.GetFeeSchedule(termId, classSectionId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("ledger", Name = "get-fee-ledger")]
        [SwaggerOperation(Summary = "Get fee ledger transactions with filters")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeeLedger([FromQuery] FeeTransactionStatus? status, [FromQuery] RequestParameters parameters)
        {
            var result = await _feeService.GetFeeLedger(status, parameters);
            return Ok(result);
        }

        [HttpGet("student/{studentId:guid}/summary", Name = "get-student-fee-summary")]
        [SwaggerOperation(Summary = "Get student fee summary and breakdown")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentFeeSummary(Guid studentId)
        {
            var result = await _feeService.GetStudentFeeSummary(studentId, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("teller", Name = "post-fee-teller")]
        [SwaggerOperation(Summary = "Post a bank teller or electronic payment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostTeller([FromBody] PostTellerRequest request)
        {
            var result = await _feeService.PostTeller(request, HttpContext.User);
            return Ok(result);
        }

        [HttpPost("teller/{id:guid}/verify", Name = "verify-fee-teller")]
        [SwaggerOperation(Summary = "Verify and issue official receipt for fee payment")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyTeller(Guid id, [FromBody] VerifyTellerRequest request)
        {
            var result = await _feeService.VerifyTeller(id, request, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("student/{studentId:guid}/receipts", Name = "get-student-receipts")]
        [SwaggerOperation(Summary = "Get receipts issued for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentReceipts(Guid studentId)
        {
            var result = await _feeService.GetStudentReceipts(studentId, HttpContext.User);
            return Ok(result);
        }

        [HttpGet("receipt/{receiptId:guid}/pdf", Name = "get-receipt-pdf")]
        [SwaggerOperation(Summary = "Get receipt PDF download payload")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReceiptPdf(Guid receiptId)
        {
            var result = await _feeService.GetReceiptPdf(receiptId, HttpContext.User);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("reminders", Name = "queue-fee-reminders")]
        [SwaggerOperation(Summary = "Queue automated fee payment reminders")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> QueuePaymentReminders([FromBody] QueuePaymentRemindersRequest request)
        {
            var result = await _feeService.QueuePaymentReminders(request);
            return Ok(result);
        }

        [HttpGet("bank-accounts", Name = "get-bank-accounts")]
        [SwaggerOperation(Summary = "Get active school bank accounts")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBankAccounts()
        {
            var result = await _feeService.GetBankAccounts();
            return Ok(result);
        }

        [HttpGet("statement/{studentId:guid}", Name = "get-student-statement")]
        [SwaggerOperation(Summary = "Get statement of account for student")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatementOfAccount(Guid studentId)
        {
            var result = await _feeService.GetStatementOfAccount(studentId, HttpContext.User);
            return Ok(result);
        }
    }
}

