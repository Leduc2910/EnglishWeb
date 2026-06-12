using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.AssignedTests;

internal static class AssignedTestsTestHelper
{
    internal static async Task<(Guid homeworkId, Guid classId)> SeedHomeworkForStudentClassAsync(
        TestApiFactory factory,
        DateTimeOffset? deadlineAt = null)
    {
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);

        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);
        var templateId = await TestTemplates.TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var teacherId = await HomeworkAssignments.HomeworkAssignmentTestHelper.GetTeacherIdAsync(factory);

        var deadline = deadlineAt ?? DateTimeOffset.UtcNow.AddDays(7);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var homework = new HomeworkAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = templateId,
            ClassId = classId,
            Status = HomeworkAssignmentStatuses.Published,
            DeadlineAt = deadline,
            TimeLimitMinutes = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.HomeworkAssignments.Add(homework);
        await db.SaveChangesAsync();

        return (homework.Id, classId);
    }

    internal static async Task<(Guid sessionId, Guid classId)> SeedLiveExamForStudentClassAsync(
        TestApiFactory factory,
        string status = LiveExamSessionStatuses.Scheduled)
    {
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);

        var classId = await Classes.ClassesTestHelper.GetDemoClassIdAsync(factory);
        var templateId = await TestTemplates.TestTemplatesTestHelper.GetDemoReadyTemplateIdAsync(factory);
        var teacherId = await HomeworkAssignments.HomeworkAssignmentTestHelper.GetTeacherIdAsync(factory);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();

        var session = new LiveExamSession
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = templateId,
            ClassId = classId,
            Status = status,
            OpenedAt = status == LiveExamSessionStatuses.Open ? DateTimeOffset.UtcNow.AddMinutes(-5) : null,
            ClosedAt = status == LiveExamSessionStatuses.Closed ? DateTimeOffset.UtcNow.AddMinutes(-1) : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.LiveExamSessions.Add(session);
        await db.SaveChangesAsync();

        return (session.Id, classId);
    }
}
