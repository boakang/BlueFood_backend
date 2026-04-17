using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManagementController : ControllerBase
{
    private readonly IBatchService _batchService;

    public ManagementController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IReadOnlyList<BatchManagementRowDto>>> GetBatches([FromQuery] string? keyword, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetBatchManagementAsync(keyword, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("certificates")]
    public async Task<ActionResult<IReadOnlyList<CertificateManagementRowDto>>> GetCertificates([FromQuery] string? keyword, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetCertificateManagementAsync(keyword, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("certificates/{certificateId:long}/batches")]
    public async Task<ActionResult<IReadOnlyList<CertificateAttachedBatchDto>>> GetBatchesByCertificateId(long certificateId, CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetBatchesByCertificateIdAsync(certificateId, cancellationToken);
        return Ok(result);
    }
}