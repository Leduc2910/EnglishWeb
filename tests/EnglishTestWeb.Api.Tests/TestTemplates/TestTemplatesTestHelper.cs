using System.Text.Json;
using EnglishTestWeb.Api.Domain.Files;
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

    internal static async Task<Guid> EnsureDraftWithMaterialsAsync(TestApiFactory factory, string skill = "reading")
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher user missing.");

        var title = $"Draft With Materials ({skill})";
        await EnsureTemplateAsync(dbContext, teacher.Id, title, skill, TemplateStatuses.Draft, null, null);

        var templateId = await dbContext.TestTemplates
            .Where(t => t.TeacherId == teacher.Id && t.Title == title)
            .Select(t => t.Id)
            .FirstAsync();

        // Add active PDF material if not already present
        var hasMaterial = await dbContext.TestMaterials
            .AnyAsync(m => m.TemplateId == templateId && m.IsActive);

        if (!hasMaterial)
        {
            var now = DateTimeOffset.UtcNow;
            var storedFile = new StoredFile
            {
                Id = Guid.NewGuid(),
                StorageKey = $"test-key-{templateId}.pdf",
                OriginalFileName = "test.pdf",
                ContentType = "application/pdf",
                SizeBytes = 1024,
                OwnerUserId = teacher.Id,
                Status = StoredFileStatuses.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.StoredFiles.Add(storedFile);

            var role = skill == "speaking" ? MaterialRoles.Audio : MaterialRoles.Pdf;
            dbContext.TestMaterials.Add(new TestMaterial
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                StoredFileId = storedFile.Id,
                Role = role,
                IsActive = true,
                CreatedAt = now
            });
            await dbContext.SaveChangesAsync();
        }

        return templateId;
    }

    internal static async Task<Guid> EnsureDraftWithCompleteAnswerKeyAsync(TestApiFactory factory)
    {
        var templateId = await EnsureDraftWithMaterialsAsync(factory, "reading");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var hasAnswerKey = await dbContext.AnswerKeyVersions.AnyAsync(a => a.TemplateId == templateId);
        if (!hasAnswerKey)
        {
            var rows = new[]
            {
                new { QuestionNumber = 1, CorrectAnswer = "A", Score = (decimal?)null },
                new { QuestionNumber = 2, CorrectAnswer = "B", Score = (decimal?)null },
                new { QuestionNumber = 3, CorrectAnswer = "C", Score = (decimal?)null }
            };
            var now = DateTimeOffset.UtcNow;
            dbContext.AnswerKeyVersions.Add(new AnswerKeyVersion
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                Status = AnswerKeyStatuses.Draft,
                ScoringMode = ScoringModes.Equal,
                QuestionCount = 3,
                TotalScore = 9m,
                RowsJson = JsonSerializer.Serialize(rows, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                CreatedAt = now,
                UpdatedAt = now
            });
            await dbContext.SaveChangesAsync();
        }

        return templateId;
    }

    internal static async Task<Guid> EnsureArchivedTemplateAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var teacher = await userManager.FindByEmailAsync(Auth.AuthTestHelper.TeacherEmail)
            ?? throw new InvalidOperationException("Teacher user missing.");

        const string title = "Archived Template For MarkReady Test";
        await EnsureTemplateAsync(
            dbContext, teacher.Id, title, TemplateSkill.Reading, TemplateStatuses.Archived,
            DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));

        return await dbContext.TestTemplates
            .Where(t => t.TeacherId == teacher.Id && t.Title == title)
            .Select(t => t.Id)
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
