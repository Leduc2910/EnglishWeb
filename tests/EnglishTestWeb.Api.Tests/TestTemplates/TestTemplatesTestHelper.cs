using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.TestTemplates;

internal static class TestTemplatesTestHelper
{
    internal const string DraftTitle = "Test Reading Draft";
    internal const string ReadyTitle = "Test Listening Ready";
    internal const string ArchivedTitle = "Test Speaking Archived";

    internal static async Task SeedDemoTemplatesAsync(TestApiFactory factory)
    {
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher user missing.");

        await EnsureTemplateAsync(
            dbContext,
            teacher.Id,
            DraftTitle,
            TemplateSkill.Reading,
            TemplateStatuses.Draft,
            null,
            null);

        await EnsureTemplateAsync(
            dbContext,
            teacher.Id,
            ReadyTitle,
            TemplateSkill.Listening,
            TemplateStatuses.Ready,
            DateTimeOffset.UtcNow.AddDays(-3),
            null);

        await EnsureTemplateAsync(
            dbContext,
            teacher.Id,
            ArchivedTitle,
            TemplateSkill.Speaking,
            TemplateStatuses.Archived,
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow.AddDays(-1));

        var otherTeacher = await userManager.FindByEmailAsync(Classes.ClassesTestHelper.OtherTeacherEmail)
            ?? throw new InvalidOperationException("Other teacher user missing.");

        await EnsureTemplateAsync(
            dbContext,
            otherTeacher.Id,
            "Other Teacher Template",
            TemplateSkill.Reading,
            TemplateStatuses.Ready,
            null,
            null);
    }

    internal static async Task<Guid> GetDemoDraftTemplateIdAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        return await dbContext.TestTemplates
            .Where(entity => entity.Title == DraftTitle)
            .Select(entity => entity.Id)
            .FirstAsync();
    }

    internal static async Task<Guid> EnsureSpeakingDraftTemplateAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher user missing.");

        const string title = "Test Speaking Draft";
        await EnsureTemplateAsync(
            dbContext,
            teacher.Id,
            title,
            TemplateSkill.Speaking,
            TemplateStatuses.Draft,
            null,
            null);

        return await dbContext.TestTemplates
            .Where(entity => entity.TeacherId == teacher.Id && entity.Title == title)
            .Select(entity => entity.Id)
            .FirstAsync();
    }

    internal static async Task<Guid> GetDemoReadyTemplateIdAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        return await dbContext.TestTemplates
            .Where(entity => entity.Title == ReadyTitle)
            .Select(entity => entity.Id)
            .FirstAsync();
    }

    private static async Task EnsureTemplateAsync(
        EnglishTestWebDbContext dbContext,
        string teacherId,
        string title,
        string skill,
        string status,
        DateTimeOffset? lastUsedAt,
        DateTimeOffset? archivedAt)
    {
        var existing = await dbContext.TestTemplates
            .FirstOrDefaultAsync(entity => entity.TeacherId == teacherId && entity.Title == title);

        if (existing is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.TestTemplates.Add(new TestTemplate
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            Title = title,
            Skill = skill,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
            LastUsedAt = lastUsedAt,
            ArchivedAt = archivedAt
        });
        await dbContext.SaveChangesAsync();
    }
}
