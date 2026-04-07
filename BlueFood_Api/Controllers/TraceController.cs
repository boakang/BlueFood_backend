using BlueFood.Api.Models;
using BlueFood.Api.Infrastructure;
using BlueFood.Api.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TraceController : ControllerBase
{
    private readonly IBatchService _batchService;

    public TraceController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("{qrToken}")]
    public async Task<ActionResult<IReadOnlyList<TraceEventDto>>> GetByQrToken(string qrToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return BadRequest("qrToken is required.");
        }

        var result = await _batchService.GetTraceByQrTokenAsync(qrToken, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{qrToken}/qrcode")]
    public async Task<IActionResult> GetQrCodeImage(string qrToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return BadRequest("qrToken is required.");
        }

        var traceRows = await _batchService.GetTraceByQrTokenAsync(qrToken, cancellationToken);
        if (traceRows.Count == 0)
        {
            return NotFound("QR token not found.");
        }

        var publicTraceUrl = PublicTraceUrlBuilder.Build(qrToken);

        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(publicTraceUrl, QRCodeGenerator.ECCLevel.H);
        var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(24);

        return File(pngBytes, "image/png");
    }

}
