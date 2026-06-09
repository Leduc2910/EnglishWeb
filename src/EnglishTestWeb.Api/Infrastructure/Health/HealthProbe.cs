using EnglishTestWeb.Api.Application.Common;

namespace EnglishTestWeb.Api.Infrastructure.Health;

public sealed class HealthProbe : IHealthProbe
{
    public HealthSnapshot GetSnapshot() => new("ok", "EnglishTestWeb.Api");
}
