using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IBatchService _batchService;

    public DashboardController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetDashboardOverviewAsync(cancellationToken);
        return Ok(result);
    }
}
