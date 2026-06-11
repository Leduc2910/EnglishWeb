using EnglishTestWeb.Api.Application.HomeworkAssignments;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.HomeworkAssignments;
using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishTestWeb.Api.Infrastructure.HomeworkAssignments;

public sealed class HomeworkAssignmentService(
    EnglishTestWebDbContext dbContext,
    ITemplateAuthorizationService templateAuthorizationService,
    IClassAuthorizationService classAuthorizationService,
    ILogger<HomeworkAssignmentService> logger) : IHomeworkAssignmentService
{
    public async Task<CreateHomeworkAssignmentResult> CreateAsync(
        string teacherId,
        CreateHomeworkAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Template ownership check
        var templateDecision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            request.TemplateId, teacherId, cancellationToken);

        if (!templateDecision.IsAllowed)
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.templateNotFound", StatusCodes.Status404NotFound);
        }

        // 2. Template must be Ready
        var template = await dbContext.TestTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template is null)
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.templateNotFound", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(template.Status, TemplateStatuses.Ready, StringComparison.Ordinal))
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.templateNotReady", StatusCodes.Status400BadRequest);
        }

        // 3. Class ownership check
        var classDecision = await classAuthorizationService.RequireTeacherClassAccessAsync(
            request.ClassId, teacherId, cancellationToken);

        if (!classDecision.IsAllowed)
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.classNotFound", StatusCodes.Status404NotFound);
        }

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClassId, cancellationToken);

        if (schoolClass is null)
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.classNotFound", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(schoolClass.Status, ClassStatuses.Active, StringComparison.Ordinal))
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.classNotActive", StatusCodes.Status400BadRequest);
        }

        // 4. Deadline validation — must be at least 1 minute in future
        var now = DateTimeOffset.UtcNow;
        if (request.DeadlineAt <= now.AddMinutes(1))
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.deadlinePast", StatusCodes.Status400BadRequest);
        }

        // 5. TimeLimitMinutes validation
        if (request.TimeLimitMinutes.HasValue &&
            (request.TimeLimitMinutes.Value < 1 || request.TimeLimitMinutes.Value > 600))
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.timeLimitInvalid", StatusCodes.Status400BadRequest);
        }

        // 6. Create + save
        var assignment = new HomeworkAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = request.TemplateId,
            ClassId = request.ClassId,
            Status = HomeworkAssignmentStatuses.Published,
            DeadlineAt = request.DeadlineAt,
            TimeLimitMinutes = request.TimeLimitMinutes,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.HomeworkAssignments.Add(assignment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CreateHomeworkAssignmentResult(false, null, "homework.createFailed", StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation(
            "HomeworkAssignmentCreated: assignmentId={AssignmentId} templateId={TemplateId} classId={ClassId} teacherId={TeacherId} status={Status} deadlineAt={DeadlineAt} at={Timestamp}",
            assignment.Id, request.TemplateId, request.ClassId, teacherId, assignment.Status, request.DeadlineAt, now);

        var response = new HomeworkAssignmentResponse(
            assignment.Id,
            template.Id,
            template.Title,
            template.Skill,
            schoolClass.Id,
            schoolClass.Name,
            assignment.DeadlineAt,
            assignment.TimeLimitMinutes,
            assignment.Status,
            assignment.CreatedAt);

        return new CreateHomeworkAssignmentResult(true, response, null, StatusCodes.Status201Created);
    }
}
