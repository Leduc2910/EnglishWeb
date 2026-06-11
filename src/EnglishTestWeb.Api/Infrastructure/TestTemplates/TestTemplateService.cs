using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Contracts.TestTemplates;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.TestTemplates;

public sealed class TestTemplateService(
    EnglishTestWebDbContext dbContext,
    ITemplateAuthorizationService templateAuthorizationService) : ITestTemplateService
{
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
