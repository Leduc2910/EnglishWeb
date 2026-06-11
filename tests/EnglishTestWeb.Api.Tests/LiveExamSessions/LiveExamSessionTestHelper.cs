using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.LiveExamSessions;

internal static class LiveExamSessionTestHelper
{
    internal static async Task<(Guid templateId, Guid classId)> EnsureReadyTemplateAndClassAsync(TestApiFactory factory)
    {
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var templateId = await TestTemplates.TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);
        return (templateId, classId);
    }

    internal static async Task<(Guid templateId, Guid classId)> EnsureDraftTemplateAndClassAsync(TestApiFactory factory)
    {
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var templateId = await TestTemplates.TestTemplatesTestHelper.GetDemoDraftTemplateIdAsync(factory);
        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);
        return (templateId, classId);
    }

    internal static async Task<(Guid templateId, Guid classId)> EnsureReadyTemplateAndInactiveClassAsync(TestApiFactory factory)
    {
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        var templateId = await TestTemplates.TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<EnglishTestWeb.Api.Domain.Identity.ApplicationUser>>();
        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher not found.");

        var inactiveClass = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Live Exam Class",
            ClassCode = "INACT2",
            TeacherId = teacher.Id,
            Status = ClassStatuses.Inactive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Classes.Add(inactiveClass);
        await dbContext.SaveChangesAsync();

        return (templateId, inactiveClass.Id);
    }

    internal static async Task<Guid> CreateScheduledSessionAsync(
        TestApiFactory factory,
        HttpClient client,
        Guid templateId,
        Guid classId)
    {
        var response = await Auth.AuthTestHelper.PostJsonAsync(client, "/api/live-exam-sessions", new
        {
            templateId,
            classId,
            scheduledStartAt = (DateTimeOffset?)null,
            scheduledEndAt = (DateTimeOffset?)null
        });

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Failed to create session: {response.StatusCode}");
        }

        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        return document.RootElement.GetProperty("id").GetGuid();
    }
}
