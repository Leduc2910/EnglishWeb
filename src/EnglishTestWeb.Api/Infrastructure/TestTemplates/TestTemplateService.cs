using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Contracts.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
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

        return new TestTemplateAccessResult(
            true,
            new TestTemplateDetailResponse(
                template.Id,
                template.Title,
                template.Skill,
                template.Description,
                template.Status,
                template.CreatedAt,
                template.UpdatedAt,
                template.LastUsedAt,
                template.ArchivedAt),
            null);
    }
}
