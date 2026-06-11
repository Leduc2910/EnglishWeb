using EnglishTestWeb.Api.Application.LiveExamSessions;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Contracts.LiveExamSessions;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishTestWeb.Api.Infrastructure.LiveExamSessions;

public sealed class LiveExamSessionService(
    EnglishTestWebDbContext dbContext,
    ITemplateAuthorizationService templateAuthorizationService,
    IClassAuthorizationService classAuthorizationService,
    ILogger<LiveExamSessionService> logger) : ILiveExamSessionService
{
    public async Task<CreateLiveExamSessionResult> CreateAsync(
        string teacherId,
        CreateLiveExamSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Template ownership check
        var templateDecision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            request.TemplateId, teacherId, cancellationToken);

        if (!templateDecision.IsAllowed)
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.templateNotFound", StatusCodes.Status404NotFound);
        }

        // 2. Template must be Ready
        var template = await dbContext.TestTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template is null)
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.templateNotFound", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(template.Status, TemplateStatuses.Ready, StringComparison.Ordinal))
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.templateNotReady", StatusCodes.Status400BadRequest);
        }

        // 3. Class ownership check
        var classDecision = await classAuthorizationService.RequireTeacherClassAccessAsync(
            request.ClassId, teacherId, cancellationToken);

        if (!classDecision.IsAllowed)
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.classNotFound", StatusCodes.Status404NotFound);
        }

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClassId, cancellationToken);

        if (schoolClass is null)
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.classNotFound", StatusCodes.Status404NotFound);
        }

        // 4. Class must be Active
        if (!string.Equals(schoolClass.Status, ClassStatuses.Active, StringComparison.Ordinal))
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.classNotActive", StatusCodes.Status400BadRequest);
        }

        // 5. Create + save
        var now = DateTimeOffset.UtcNow;
        var session = new LiveExamSession
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            TestTemplateId = request.TemplateId,
            ClassId = request.ClassId,
            Status = LiveExamSessionStatuses.Scheduled,
            ScheduledStartAt = request.ScheduledStartAt,
            ScheduledEndAt = request.ScheduledEndAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.LiveExamSessions.Add(session);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CreateLiveExamSessionResult(false, null, "liveExam.createFailed", StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation(
            "LiveExamSessionCreated: sessionId={SessionId} templateId={TemplateId} classId={ClassId} teacherId={TeacherId} status={Status} at={Timestamp}",
            session.Id, request.TemplateId, request.ClassId, teacherId, session.Status, now);

        return new CreateLiveExamSessionResult(true, MapResponse(session, template, schoolClass), null, StatusCodes.Status201Created);
    }

    public async Task<LiveExamSessionTransitionResult> OpenAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = await dbContext.LiveExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TeacherId == teacherId, cancellationToken);

        if (session is null)
        {
            return new LiveExamSessionTransitionResult(false, null, "liveExam.sessionNotFound", StatusCodes.Status404NotFound);
        }

        if (string.Equals(session.Status, LiveExamSessionStatuses.Open, StringComparison.Ordinal))
        {
            return new LiveExamSessionTransitionResult(false, null, "liveExam.alreadyOpen", StatusCodes.Status409Conflict);
        }

        if (string.Equals(session.Status, LiveExamSessionStatuses.Closed, StringComparison.Ordinal))
        {
            return new LiveExamSessionTransitionResult(false, null, "liveExam.sessionClosed", StatusCodes.Status409Conflict);
        }

        // Status is "scheduled" — transition to open
        var previousStatus = session.Status;
        var now = DateTimeOffset.UtcNow;
        session.Status = LiveExamSessionStatuses.Open;
        session.OpenedAt = now;
        session.UpdatedAt = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(session).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            return new LiveExamSessionTransitionResult(false, null, "liveExam.transitionFailed", StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation(
            "LiveExamSessionOpened: sessionId={SessionId} teacherId={TeacherId} previousStatus={PreviousStatus} newStatus={NewStatus} at={Timestamp}",
            session.Id, teacherId, previousStatus, session.Status, now);

        var (template, schoolClass) = await LoadRelatedEntitiesAsync(session, cancellationToken);
        return new LiveExamSessionTransitionResult(true, MapResponse(session, template, schoolClass), null, StatusCodes.Status200OK);
    }

    public async Task<LiveExamSessionTransitionResult> CloseAsync(
        string teacherId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = await dbContext.LiveExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TeacherId == teacherId, cancellationToken);

        if (session is null)
        {
            return new LiveExamSessionTransitionResult(false, null, "liveExam.sessionNotFound", StatusCodes.Status404NotFound);
        }

        if (string.Equals(session.Status, LiveExamSessionStatuses.Closed, StringComparison.Ordinal))
        {
            return new LiveExamSessionTransitionResult(false, null, "liveExam.alreadyClosed", StatusCodes.Status409Conflict);
        }

        if (string.Equals(session.Status, LiveExamSessionStatuses.Scheduled, StringComparison.Ordinal))
        {
            return new LiveExamSessionTransitionResult(false, null, "liveExam.sessionNotOpen", StatusCodes.Status409Conflict);
        }

        // Status is "open" — transition to closed
        var previousStatus = session.Status;
        var now = DateTimeOffset.UtcNow;
        session.Status = LiveExamSessionStatuses.Closed;
        session.ClosedAt = now;
        session.UpdatedAt = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(session).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            return new LiveExamSessionTransitionResult(false, null, "liveExam.transitionFailed", StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation(
            "LiveExamSessionClosed: sessionId={SessionId} teacherId={TeacherId} previousStatus={PreviousStatus} newStatus={NewStatus} at={Timestamp}",
            session.Id, teacherId, previousStatus, session.Status, now);

        var (template, schoolClass) = await LoadRelatedEntitiesAsync(session, cancellationToken);
        return new LiveExamSessionTransitionResult(true, MapResponse(session, template, schoolClass), null, StatusCodes.Status200OK);
    }

    private async Task<(Domain.TestTemplates.TestTemplate template, Domain.Classes.SchoolClass schoolClass)> LoadRelatedEntitiesAsync(
        LiveExamSession session, CancellationToken cancellationToken)
    {
        var template = await dbContext.TestTemplates
            .AsNoTracking()
            .FirstAsync(t => t.Id == session.TestTemplateId, cancellationToken);

        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .FirstAsync(c => c.Id == session.ClassId, cancellationToken);

        return (template, schoolClass);
    }

    private static LiveExamSessionResponse MapResponse(
        LiveExamSession session,
        Domain.TestTemplates.TestTemplate template,
        Domain.Classes.SchoolClass schoolClass)
    {
        return new LiveExamSessionResponse(
            session.Id,
            template.Id,
            template.Title,
            template.Skill,
            schoolClass.Id,
            schoolClass.Name,
            session.Status,
            session.ScheduledStartAt,
            session.ScheduledEndAt,
            session.OpenedAt,
            session.ClosedAt,
            session.CreatedAt);
    }
}
