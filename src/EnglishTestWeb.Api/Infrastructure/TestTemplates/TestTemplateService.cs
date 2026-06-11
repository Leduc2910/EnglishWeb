using System.Text.Json;
using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Contracts.TestTemplates;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishTestWeb.Api.Infrastructure.TestTemplates;

public sealed class TestTemplateService(
    EnglishTestWebDbContext dbContext,
    ITemplateAuthorizationService templateAuthorizationService,
    ILogger<TestTemplateService> logger) : ITestTemplateService
{
    private static readonly JsonSerializerOptions RowsJsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<IReadOnlyList<TestTemplateListItemResponse>> ListForTeacherAsync(
        string teacherId,
        TestTemplateListQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 50 : Math.Min(query.PageSize, 100);

        var templatesQuery = dbContext.TestTemplates
            .AsNoTracking()
            .Where(entity => entity.TeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(query.Skill))
        {
            var skill = query.Skill.Trim().ToLowerInvariant();
            templatesQuery = templatesQuery.Where(entity => entity.Skill == skill);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            templatesQuery = templatesQuery.Where(entity => entity.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var search = query.Q.Trim();
            templatesQuery = templatesQuery.Where(entity => entity.Title.Contains(search));
        }

        return await templatesQuery
            .OrderByDescending(entity => entity.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new TestTemplateListItemResponse(
                entity.Id,
                entity.Title,
                entity.Skill,
                entity.Status,
                entity.LastUsedAt,
                entity.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TestTemplateAccessResult> GetByIdForTeacherAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        if (!decision.IsAllowed)
        {
            return new TestTemplateAccessResult(false, null, decision.ErrorCode ?? "templates.notFound");
        }

        var template = await dbContext.TestTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == templateId, cancellationToken);

        if (template is null)
        {
            return new TestTemplateAccessResult(false, null, "templates.notFound");
        }

        return new TestTemplateAccessResult(true, MapDetail(template), null);
    }

    public async Task<TestTemplateMutationResult> CreateDraftForTeacherAsync(
        string teacherId,
        CreateTestTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validationError = ValidateSetupRequest(request.Title, request.Skill, request.Description, request.Tags, out var normalizedTags);
        if (validationError is not null)
        {
            return new TestTemplateMutationResult(false, null, validationError, StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        var template = new TestTemplate
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            Title = request.Title.Trim(),
            Skill = request.Skill.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            TagsJson = TestTemplateTagsSerializer.Serialize(normalizedTags),
            Status = TemplateStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.TestTemplates.Add(template);
        return await SaveTemplateMutationAsync(template, StatusCodes.Status201Created, cancellationToken);
    }

    public async Task<TestTemplateMutationResult> UpdateDraftSetupForTeacherAsync(
        Guid templateId,
        string teacherId,
        UpdateTestTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var decision = await templateAuthorizationService.RequireTeacherTemplateAccessAsync(
            templateId,
            teacherId,
            cancellationToken);

        if (!decision.IsAllowed)
        {
            return new TestTemplateMutationResult(
                false,
                null,
                decision.ErrorCode ?? "templates.notFound",
                StatusCodes.Status404NotFound);
        }

        var template = await dbContext.TestTemplates
            .FirstOrDefaultAsync(entity => entity.Id == templateId, cancellationToken);

        if (template is null)
        {
            return new TestTemplateMutationResult(false, null, "templates.notFound", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(template.Status, TemplateStatuses.Draft, StringComparison.Ordinal))
        {
            return new TestTemplateMutationResult(false, null, "templates.notEditable", StatusCodes.Status409Conflict);
        }

        var validationError = ValidateSetupRequest(request.Title, request.Skill, request.Description, request.Tags, out var normalizedTags);
        if (validationError is not null)
        {
            return new TestTemplateMutationResult(false, null, validationError, StatusCodes.Status400BadRequest);
        }

        template.Title = request.Title.Trim();
        template.Skill = request.Skill.Trim().ToLowerInvariant();
        template.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        template.TagsJson = TestTemplateTagsSerializer.Serialize(normalizedTags);
        template.UpdatedAt = DateTimeOffset.UtcNow;

        return await SaveTemplateMutationAsync(template, StatusCodes.Status200OK, cancellationToken);
    }

    public async Task<MarkReadyResult> MarkReadyAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var template = await dbContext.TestTemplates
            .FirstOrDefaultAsync(entity => entity.Id == templateId, cancellationToken);

        if (template is null)
        {
            return new MarkReadyResult(false, null, "templates.notFound", StatusCodes.Status404NotFound);
        }

        if (string.Equals(template.Status, TemplateStatuses.Archived, StringComparison.Ordinal))
        {
            return new MarkReadyResult(false, null, "templates.archived", StatusCodes.Status409Conflict);
        }

        // Idempotent: already ready — return current state without any change
        if (string.Equals(template.Status, TemplateStatuses.Ready, StringComparison.Ordinal))
        {
            return new MarkReadyResult(true, MapDetail(template), null, StatusCodes.Status200OK);
        }

        // Defensive check: template info
        if (string.IsNullOrWhiteSpace(template.Title) || string.IsNullOrWhiteSpace(template.Skill))
        {
            return new MarkReadyResult(false, null, "review.templateInfoMissing", StatusCodes.Status400BadRequest);
        }

        var isReadingOrListening =
            string.Equals(template.Skill, TemplateSkill.Reading, StringComparison.Ordinal) ||
            string.Equals(template.Skill, TemplateSkill.Listening, StringComparison.Ordinal);
        var isSpeaking = string.Equals(template.Skill, TemplateSkill.Speaking, StringComparison.Ordinal);

        // Material check
        if (isReadingOrListening)
        {
            var hasPdf = await dbContext.TestMaterials
                .AnyAsync(m => m.TemplateId == templateId && m.Role == MaterialRoles.Pdf && m.IsActive, cancellationToken);
            if (!hasPdf)
            {
                return new MarkReadyResult(false, null, "review.missingRequiredMaterial", StatusCodes.Status400BadRequest);
            }
        }

        if (isSpeaking)
        {
            var hasMaterial = await dbContext.TestMaterials
                .AnyAsync(m => m.TemplateId == templateId && m.IsActive, cancellationToken);
            if (!hasMaterial)
            {
                return new MarkReadyResult(false, null, "review.missingRequiredMaterial", StatusCodes.Status400BadRequest);
            }
        }

        if (!isReadingOrListening && !isSpeaking)
        {
            return new MarkReadyResult(false, null, "review.missingRequiredMaterial", StatusCodes.Status400BadRequest);
        }

        // AnswerKey check (reading/listening only)
        AnswerKeyVersion? answerKey = null;
        if (isReadingOrListening)
        {
            answerKey = await dbContext.AnswerKeyVersions
                .FirstOrDefaultAsync(a => a.TemplateId == templateId, cancellationToken);

            if (answerKey is null || answerKey.QuestionCount < 1)
            {
                return new MarkReadyResult(false, null, "review.answerKeyIncomplete", StatusCodes.Status400BadRequest);
            }

            List<AnswerKeyRow> rows;
            try
            {
                rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(answerKey.RowsJson, RowsJsonOptions) ?? [];
            }
            catch (JsonException)
            {
                rows = [];
            }

            if (rows.Count != answerKey.QuestionCount || rows.Any(r => string.IsNullOrWhiteSpace(r.CorrectAnswer)))
            {
                return new MarkReadyResult(false, null, "review.answerKeyIncomplete", StatusCodes.Status400BadRequest);
            }

            // Scoring check
            if (string.Equals(answerKey.ScoringMode, ScoringModes.Equal, StringComparison.Ordinal))
            {
                if (answerKey.TotalScore is null || answerKey.TotalScore <= 0)
                {
                    return new MarkReadyResult(false, null, "review.scoringInvalid", StatusCodes.Status400BadRequest);
                }
            }
            else if (string.Equals(answerKey.ScoringMode, ScoringModes.PerQuestion, StringComparison.Ordinal))
            {
                if (rows.Any(r => r.Score is null || r.Score <= 0))
                {
                    return new MarkReadyResult(false, null, "review.scoringInvalid", StatusCodes.Status400BadRequest);
                }
            }
        }

        // Transition — one SaveChangesAsync for atomicity
        var now = DateTimeOffset.UtcNow;
        var previousStatus = template.Status;
        template.Status = TemplateStatuses.Ready;
        template.UpdatedAt = now;

        if (answerKey is not null)
        {
            answerKey.Status = AnswerKeyStatuses.Ready;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new MarkReadyResult(false, null, "review.markReadyFailed", StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation(
            "TemplateMarkedReady: templateId={TemplateId} teacherId={TeacherId} previousStatus={PreviousStatus} newStatus={NewStatus} at={Timestamp}",
            templateId, teacherId, previousStatus, TemplateStatuses.Ready, now);

        return new MarkReadyResult(true, MapDetail(template), null, StatusCodes.Status200OK);
    }

    private async Task<TestTemplateMutationResult> SaveTemplateMutationAsync(
        TestTemplate template,
        int successStatusCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new TestTemplateMutationResult(true, MapSetup(template), null, successStatusCode);
        }
        catch (DbUpdateException)
        {
            return new TestTemplateMutationResult(
                false,
                null,
                "templates.tagLimit",
                StatusCodes.Status400BadRequest);
        }
    }

    private static string? ValidateSetupRequest(
        string title,
        string skill,
        string? description,
        IReadOnlyList<string>? tags,
        out IReadOnlyList<string> normalizedTags)
    {
        normalizedTags = [];

        var titleError = TestTemplateSetupValidation.ValidateTitle(title);
        if (titleError is not null)
        {
            return titleError;
        }

        var skillError = TestTemplateSetupValidation.ValidateSkill(skill);
        if (skillError is not null)
        {
            return skillError;
        }

        var descriptionError = TestTemplateSetupValidation.ValidateDescription(description);
        if (descriptionError is not null)
        {
            return descriptionError;
        }

        var tagError = TestTemplateSetupValidation.ValidateTags(tags, out normalizedTags);
        if (tagError is not null)
        {
            return tagError;
        }

        return TestTemplateTagsSerializer.ValidateSerializedLength(normalizedTags);
    }

    private static TestTemplateDetailResponse MapDetail(TestTemplate template) =>
        new(
            template.Id,
            template.Title,
            template.Skill,
            template.Description,
            TestTemplateTagsSerializer.Deserialize(template.TagsJson),
            template.Status,
            template.CreatedAt,
            template.UpdatedAt,
            template.LastUsedAt,
            template.ArchivedAt);

    private static TestTemplateSetupResponse MapSetup(TestTemplate template) =>
        new(
            template.Id,
            template.Title,
            template.Skill,
            template.Description,
            TestTemplateTagsSerializer.Deserialize(template.TagsJson),
            template.Status,
            template.CreatedAt,
            template.UpdatedAt,
            template.LastUsedAt,
            template.ArchivedAt);
}
