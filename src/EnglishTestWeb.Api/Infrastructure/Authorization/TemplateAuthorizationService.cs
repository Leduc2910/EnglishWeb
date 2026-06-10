using EnglishTestWeb.Api.Application.Security;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Authorization;

public sealed class TemplateAuthorizationService(EnglishTestWebDbContext dbContext) : ITemplateAuthorizationService
{
    public async Task<AuthorizationDecision> RequireTeacherTemplateAccessAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var template = await dbContext.TestTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == templateId, cancellationToken);

        if (template is null || template.TeacherId != teacherId)
        {
            return AuthorizationDecision.HiddenNotFound(
                "templates.notFound",
                AuthorizationDenialReason.TemplateOwnership);
        }

        return AuthorizationDecision.Allow();
    }
}
