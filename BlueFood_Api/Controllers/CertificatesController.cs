using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly IBatchService _batchService;

    public CertificatesController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateCertificate([FromBody] CreateCertificateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CertificateCode) || string.IsNullOrWhiteSpace(request.CertificateName) || string.IsNullOrWhiteSpace(request.Actor))
        {
            return BadRequest("CertificateCode, CertificateName, Actor are required.");
        }

        var certificateId = await _batchService.CreateCertificateAsync(request, cancellationToken);
        return Ok(new { certificateId });
    }
}
