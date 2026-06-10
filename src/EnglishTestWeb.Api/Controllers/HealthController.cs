using EnglishTestWeb.Api.Application.Common;
using EnglishTestWeb.Api.Contracts.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController(IHealthProbe healthProbe, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        var snapshot = healthProbe.GetSnapshot();
        return Ok(new HealthResponse(snapshot.Status, snapshot.Application));
    }

    [HttpPost("unsafe-smoke")]
    public ActionResult UnsafeSmoke()
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        var snapshot = healthProbe.GetSnapshot();
        return Ok(new HealthResponse(snapshot.Status, snapshot.Application));
    }
}
