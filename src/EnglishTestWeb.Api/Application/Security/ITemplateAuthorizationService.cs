namespace EnglishTestWeb.Api.Application.Security;

public interface ITemplateAuthorizationService
{
    Task<AuthorizationDecision> RequireTeacherTemplateAccessAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default);
}
