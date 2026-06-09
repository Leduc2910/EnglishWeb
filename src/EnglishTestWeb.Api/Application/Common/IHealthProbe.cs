namespace EnglishTestWeb.Api.Application.Common;

public interface IHealthProbe
{
    HealthSnapshot GetSnapshot();
}
