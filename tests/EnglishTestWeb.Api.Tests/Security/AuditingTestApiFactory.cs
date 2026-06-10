using EnglishTestWeb.Api.Infrastructure.Audit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishTestWeb.Api.Tests.Security;

public sealed class AuditingTestApiFactory : TestApiFactory
{
    public FakeAuthorizationAuditLogger AuditLogger { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthorizationAuditLogger>();
            services.AddSingleton(AuditLogger);
            services.AddSingleton<IAuthorizationAuditLogger>(sp => sp.GetRequiredService<FakeAuthorizationAuditLogger>());
        });
    }
}
