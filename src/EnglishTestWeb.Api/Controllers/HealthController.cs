using EnglishTestWeb.Api.Application.Common;
using EnglishTestWeb.Api.Contracts.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController(IHealthProbe healthProbe) : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        var snapshot = healthProbe.GetSnapshot();
        return Ok(new HealthResponse(snapshot.Status, snapshot.Application));
    }

    [HttpPost("unsafe-smoke")]
    public ActionResult<HealthResponse> UnsafeSmoke()
    {
        var snapshot = healthProbe.GetSnapshot();
        return Ok(new HealthResponse(snapshot.Status, snapshot.Application));
    }
}
