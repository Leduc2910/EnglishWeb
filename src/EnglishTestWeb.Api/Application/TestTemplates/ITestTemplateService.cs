using EnglishTestWeb.Api.Contracts.TestTemplates;

namespace EnglishTestWeb.Api.Application.TestTemplates;

public sealed record TestTemplateAccessResult(
    bool Allowed,
    TestTemplateDetailResponse? Detail,
    string? ErrorCode);

public interface ITestTemplateService
{
    Task<IReadOnlyList<TestTemplateListItemResponse>> ListForTeacherAsync(
        string teacherId,
        TestTemplateListQuery query,
        CancellationToken cancellationToken = default);

    Task<TestTemplateAccessResult> GetByIdForTeacherAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default);
}
