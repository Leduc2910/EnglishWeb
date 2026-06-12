using EnglishTestWeb.Api.Application.AssignedTests;
using EnglishTestWeb.Api.Application.Speaking;
using EnglishTestWeb.Api.Application.Submissions;
using EnglishTestWeb.Api.Infrastructure.Speaking;
using EnglishTestWeb.Api.Infrastructure.Submissions;
using EnglishTestWeb.Api.Application.Auth;
using EnglishTestWeb.Api.Application.Classes;
using EnglishTestWeb.Api.Application.HomeworkAssignments;
using EnglishTestWeb.Api.Application.LiveExamSessions;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Application.Common;
using EnglishTestWeb.Api.Application.Files;
using EnglishTestWeb.Api.Application.Identity;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Audit;
using EnglishTestWeb.Api.Infrastructure.Authorization;
using EnglishTestWeb.Api.Infrastructure.Authorization.Handlers;
using EnglishTestWeb.Api.Infrastructure.Authorization.Policies;
using EnglishTestWeb.Api.Infrastructure.AssignedTests;
using EnglishTestWeb.Api.Infrastructure.Classes;
using EnglishTestWeb.Api.Infrastructure.Files;
using EnglishTestWeb.Api.Infrastructure.HomeworkAssignments;
using EnglishTestWeb.Api.Infrastructure.LiveExamSessions;
using EnglishTestWeb.Api.Infrastructure.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Health;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Infrastructure.Security;
using EnglishTestWeb.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 110_000_000; // 100MB speaking files + overhead
});
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
    if (builder.Environment.IsEnvironment("Testing"))
    {
        var testingDatabaseName = builder.Configuration["Testing:DatabaseName"] ?? "EnglishTestWeb_Tests";
        options.UseInMemoryDatabase(testingDatabaseName);
        options.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        return;
    }

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
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return ApiAuthChallengeWriter.WriteUnauthorizedAsync(context.HttpContext);
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return ApiAuthChallengeWriter.WriteForbiddenAsync(context.HttpContext);
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
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
builder.Services.Configure<IdentityDevUserOptions>(builder.Configuration.GetSection(IdentityDevUserOptions.SectionName));
builder.Services.Configure<MvpDemoDataOptions>(builder.Configuration.GetSection(MvpDemoDataOptions.SectionName));
builder.Services.AddScoped<IFileStorage, LocalProtectedFileStorage>();
builder.Services.AddScoped<IHealthProbe, HealthProbe>();
builder.Services.AddScoped<IIdentityRoleSeeder, IdentityRoleSeeder>();
builder.Services.AddScoped<IIdentityDevUserSeeder, IdentityDevUserSeeder>();
builder.Services.AddScoped<IMvpDemoDataSeeder, MvpDemoDataSeeder>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IClassAuthorizationService, ClassAuthorizationService>();
builder.Services.AddScoped<ITemplateAuthorizationService, TemplateAuthorizationService>();
builder.Services.AddScoped<IHiddenResourceResponseFactory, HiddenResourceResponseFactory>();
builder.Services.AddScoped<IAuthorizationAuditLogger, AuthorizationAuditLogger>();
builder.Services.AddScoped<AuthorizationDenialAuditor>();
builder.Services.AddScoped<IAuthorizationHandler, ClassTeacherAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ClassStudentAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TemplateTeacherAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TemplateTeacherEditAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.CanViewClassAsTeacher, policy =>
    {
        policy.RequireRole(IdentityRoleNames.Teacher);
        policy.AddRequirements(new ClassTeacherViewRequirement());
    });
    options.AddPolicy(AuthorizationPolicies.CanViewClassAsStudent, policy =>
    {
        policy.RequireRole(IdentityRoleNames.Student);
        policy.AddRequirements(new ClassStudentViewRequirement());
    });
    options.AddPolicy(AuthorizationPolicies.CanViewTemplateAsTeacher, policy =>
    {
        policy.RequireRole(IdentityRoleNames.Teacher);
        policy.AddRequirements(new TemplateTeacherViewRequirement());
    });
    options.AddPolicy(AuthorizationPolicies.CanEditTemplateAsTeacher, policy =>
    {
        policy.RequireRole(IdentityRoleNames.Teacher);
        policy.AddRequirements(new TemplateTeacherEditRequirement());
    });
});
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ITestTemplateService, TestTemplateService>();
builder.Services.AddScoped<ITestTemplateMaterialService, TestTemplateMaterialService>();
builder.Services.AddScoped<IAnswerKeyService, AnswerKeyService>();
builder.Services.AddScoped<IProtectedFileService, ProtectedFileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IXsrfTokenService, XsrfTokenService>();
builder.Services.AddScoped<IHomeworkAssignmentService, HomeworkAssignmentService>();
builder.Services.AddScoped<ILiveExamSessionService, LiveExamSessionService>();
builder.Services.AddScoped<IAssignedTestService, AssignedTestService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ISpeakingSubmissionService, SpeakingSubmissionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()
    && !app.Environment.IsEnvironment("Testing")
    && string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    throw new InvalidOperationException("DataProtection:KeysPath is required outside Development/Testing.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentLocalhost");
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseApiXsrfProtection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Configuration.GetValue<bool>("Identity:SeedRolesOnStartup")
    || app.Configuration.GetValue<bool>("Identity:SeedDevTeacherOnStartup")
    || app.Configuration.GetValue<bool>("Identity:SeedMvpDemoOnStartup")
    || args.Contains("--seed-identity-roles")
    || args.Contains("--seed-dev-teacher")
    || args.Contains("--seed-mvp-demo"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>().SeedAsync();

    if (app.Configuration.GetValue<bool>("Identity:SeedDevTeacherOnStartup") || args.Contains("--seed-dev-teacher"))
    {
        await scope.ServiceProvider.GetRequiredService<IIdentityDevUserSeeder>().SeedAsync();
    }

    if (app.Configuration.GetValue<bool>("Identity:SeedMvpDemoOnStartup") || args.Contains("--seed-mvp-demo"))
    {
        await scope.ServiceProvider.GetRequiredService<IMvpDemoDataSeeder>().SeedAsync();
    }

    if (args.Contains("--seed-identity-roles")
        || args.Contains("--seed-dev-teacher")
        || args.Contains("--seed-mvp-demo"))
    {
        return;
    }
}

app.Run();

public partial class Program
{
}
