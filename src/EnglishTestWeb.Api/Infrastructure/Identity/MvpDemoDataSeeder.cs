using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.Identity;
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
}
