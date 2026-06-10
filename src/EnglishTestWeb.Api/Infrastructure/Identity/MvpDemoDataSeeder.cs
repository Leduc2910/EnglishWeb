using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishTestWeb.Api.Infrastructure.Identity;

public sealed class MvpDemoDataOptions
{
    public const string SectionName = "Identity:MvpDemo";

    public string StudentEmail { get; set; } = "student@englishtestweb.local";

    public string StudentPassword { get; set; } = "Student123!";

    public string StudentUserName { get; set; } = "student";

    public string ClassName { get; set; } = "English 7A";

    public string ClassCode { get; set; } = "ENG7A";
}

public interface IMvpDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class MvpDemoDataSeeder(
    UserManager<ApplicationUser> userManager,
    EnglishTestWebDbContext dbContext,
    IIdentityDevUserSeeder devTeacherSeeder,
    IOptions<MvpDemoDataOptions> options,
    IOptions<IdentityDevUserOptions> teacherOptions) : IMvpDemoDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await devTeacherSeeder.SeedAsync(cancellationToken);

        var teacherEmail = teacherOptions.Value.Email.Trim();
        var teacher = await userManager.FindByEmailAsync(teacherEmail);
        if (teacher is null)
        {
            throw new InvalidOperationException($"Dev teacher '{teacherEmail}' must exist before MVP demo seed.");
        }

        var student = await EnsureStudentAsync(options.Value, cancellationToken);
        await EnsureClassAndMembershipAsync(options.Value, teacher.Id, student.Id, cancellationToken);
        await EnsureDemoTemplatesAsync(teacher.Id, cancellationToken);
    }

    private async Task<ApplicationUser> EnsureStudentAsync(
        MvpDemoDataOptions settings,
        CancellationToken cancellationToken)
    {
        var email = settings.StudentEmail.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, IdentityRoleNames.Student))
            {
                await userManager.AddToRoleAsync(existing, IdentityRoleNames.Student);
            }

            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = settings.StudentUserName.Trim(),
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, settings.StudentPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"Failed to seed dev student user '{email}'. {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, IdentityRoleNames.Student);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"Failed to assign Student role to dev user '{email}'. {errors}");
        }

        return user;
    }

    private async Task EnsureClassAndMembershipAsync(
        MvpDemoDataOptions settings,
        string teacherId,
        string studentId,
        CancellationToken cancellationToken)
    {
        var classCode = settings.ClassCode.Trim().ToUpperInvariant();
        var schoolClass = await dbContext.Classes
            .Include(entity => entity.Memberships)
            .FirstOrDefaultAsync(entity => entity.ClassCode == classCode, cancellationToken);

        if (schoolClass is null)
        {
            schoolClass = new SchoolClass
            {
                Id = Guid.NewGuid(),
                Name = settings.ClassName.Trim(),
                ClassCode = classCode,
                TeacherId = teacherId,
                Status = ClassStatuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Classes.Add(schoolClass);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (schoolClass.TeacherId != teacherId)
        {
            schoolClass.TeacherId = teacherId;
            schoolClass.Status = ClassStatuses.Active;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var membership = schoolClass.Memberships
            .FirstOrDefault(entry => entry.StudentId == studentId);

        if (membership is null)
        {
            dbContext.ClassMemberships.Add(new ClassMembership
            {
                Id = Guid.NewGuid(),
                ClassId = schoolClass.Id,
                StudentId = studentId,
                Status = ClassStatuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (membership.Status != ClassStatuses.Active)
        {
            membership.Status = ClassStatuses.Active;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDemoTemplatesAsync(string teacherId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var seeds = new[]
        {
            new DemoTemplateSeed(
                "demo-reading-draft",
                "Reading Unit 3 — Draft",
                TemplateSkill.Reading,
                TemplateStatuses.Draft,
                null,
                null),
            new DemoTemplateSeed(
                "demo-listening-ready",
                "Listening Midterm — Ready",
                TemplateSkill.Listening,
                TemplateStatuses.Ready,
                now.AddDays(-7),
                null),
            new DemoTemplateSeed(
                "demo-speaking-archived",
                "Speaking Practice — Archived",
                TemplateSkill.Speaking,
                TemplateStatuses.Archived,
                now.AddDays(-30),
                now.AddDays(-5))
        };

        foreach (var seed in seeds)
        {
            var existing = await dbContext.TestTemplates
                .FirstOrDefaultAsync(
                    entity => entity.TeacherId == teacherId && entity.Title == seed.Title,
                    cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            dbContext.TestTemplates.Add(new TestTemplate
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                Title = seed.Title,
                Skill = seed.Skill,
                Description = $"MVP demo template ({seed.Key}).",
                Status = seed.Status,
                CreatedAt = now,
                UpdatedAt = now,
                LastUsedAt = seed.LastUsedAt,
                ArchivedAt = seed.ArchivedAt
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DemoTemplateSeed(
        string Key,
        string Title,
        string Skill,
        string Status,
        DateTimeOffset? LastUsedAt,
        DateTimeOffset? ArchivedAt);
}
