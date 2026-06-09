using EnglishTestWeb.Api.Application.Common;
using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Identity;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Health;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Infrastructure.Security;
using EnglishTestWeb.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentLocalhost", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for SQL Server Identity storage.");

builder.Services.AddDbContext<EnglishTestWebDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<EnglishTestWebDbContext>()
    .AddDefaultTokenProviders();

var useRelaxedCookieSecurePolicy = builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Testing");

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "EnglishTestWeb.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = useRelaxedCookieSecurePolicy
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "EnglishTestWeb.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = useRelaxedCookieSecurePolicy
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.HeaderName = XsrfDefaults.HeaderName;
});

var dataProtectionKeysPath = Environment.ExpandEnvironmentVariables(builder.Configuration["DataProtection:KeysPath"] ?? string.Empty);
var dataProtectionBuilder = builder.Services.AddDataProtection();
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.Configure<ProtectedStorageOptions>(builder.Configuration.GetSection(ProtectedStorageOptions.SectionName));
builder.Services.AddScoped<IFileStorage, LocalProtectedFileStorage>();
builder.Services.AddScoped<IHealthProbe, HealthProbe>();
builder.Services.AddScoped<IIdentityRoleSeeder, IdentityRoleSeeder>();
builder.Services.AddScoped<IXsrfTokenService, XsrfTokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentLocalhost");
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseApiXsrfProtection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Configuration.GetValue<bool>("Identity:SeedRolesOnStartup") || args.Contains("--seed-identity-roles"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>().SeedAsync();

    if (args.Contains("--seed-identity-roles"))
    {
        return;
    }
}

app.Run();

public partial class Program
{
}
