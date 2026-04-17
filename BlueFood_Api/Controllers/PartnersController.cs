using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartnersController : ControllerBase
{
    private readonly IBatchService _batchService;

    public PartnersController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartnerDto>>> Get([FromQuery] int? partnerType, [FromQuery] bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetPartnersAsync(partnerType, onlyActive, cancellationToken);
        return Ok(result);
    }
}
