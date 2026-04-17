using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly IBatchService _batchService;

    public BatchesController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateBatchResult>> CreateBatch([FromBody] CreateBatchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BatchCode) || string.IsNullOrWhiteSpace(request.ProductName) || string.IsNullOrWhiteSpace(request.Actor))
        {
            return BadRequest("BatchCode, ProductName, Actor are required.");
        }

        var result = await _batchService.CreateBatchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{batchCode}/events")]
    public async Task<IActionResult> AddEvent(string batchCode, [FromBody] AddBatchEventRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(batchCode) || string.IsNullOrWhiteSpace(request.EventType) || string.IsNullOrWhiteSpace(request.Actor))
        {
            return BadRequest("BatchCode, EventType, Actor are required.");
        }

        await _batchService.AddBatchEventAsync(batchCode, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{batchCode}/certificates")]
    public async Task<IActionResult> AttachCertificate(string batchCode, [FromBody] AttachCertificateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(batchCode) || request.CertificateId <= 0 || string.IsNullOrWhiteSpace(request.Actor))
        {
            return BadRequest("BatchCode, CertificateId (>0), Actor are required.");
        }

        try
        {
            await _batchService.AttachCertificateAsync(batchCode, request, cancellationToken);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Conflict("Chứng chỉ này đã được gắn cho lô hàng hiện tại.");
        }
    }

    [HttpGet("{batchCode}/certificates")]
    public async Task<ActionResult<IReadOnlyList<CertificateDto>>> GetCertificatesByBatchCode(string batchCode, CancellationToken cancellationToken)
    {
        var result = await _batchService.GetCertificatesByBatchCodeAsync(batchCode, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{batchCode}/trace")]
    public async Task<ActionResult<IReadOnlyList<TraceEventDto>>> GetTraceByBatchCode(string batchCode, CancellationToken cancellationToken)
    {
        var result = await _batchService.GetTraceByBatchCodeAsync(batchCode, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{batchCode}/audit")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditByBatchCode(string batchCode, CancellationToken cancellationToken)
    {
        var result = await _batchService.GetAuditByBatchCodeAsync(batchCode, cancellationToken);
        return Ok(result);
    }
}
