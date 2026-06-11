using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.HomeworkAssignments;

internal static class HomeworkAssignmentTestHelper
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

        var inactiveClass = new EnglishTestWeb.Api.Domain.Classes.SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Class",
            ClassCode = "INACT1",
            TeacherId = teacher.Id,
            Status = EnglishTestWeb.Api.Domain.Classes.ClassStatuses.Inactive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Classes.Add(inactiveClass);
        await dbContext.SaveChangesAsync();

        return (templateId, inactiveClass.Id);
    }

    internal static async Task<string> GetTeacherIdAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<EnglishTestWeb.Api.Domain.Identity.ApplicationUser>>();
        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher not found.");
        return teacher.Id;
    }
}
