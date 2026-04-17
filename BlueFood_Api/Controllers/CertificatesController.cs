using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.Data.SqlClient;
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

        try
        {
            var certificateId = await _batchService.CreateCertificateAsync(request, cancellationToken);
            return Ok(new { certificateId });
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Conflict("Mã chứng chỉ đã tồn tại. Hãy chọn chứng chỉ có sẵn để gắn vào lô hàng.");
        }
    }
}
