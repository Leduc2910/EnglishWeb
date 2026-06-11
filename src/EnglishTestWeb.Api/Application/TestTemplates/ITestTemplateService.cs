using EnglishTestWeb.Api.Contracts.TestTemplates;

namespace EnglishTestWeb.Api.Application.TestTemplates;

public sealed record TestTemplateAccessResult(
    bool Allowed,
    TestTemplateDetailResponse? Detail,
    string? ErrorCode);

public sealed record TestTemplateMutationResult(
    bool Succeeded,
    TestTemplateSetupResponse? Response,
    string? ErrorCode,
    int StatusCode);

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

    Task<TestTemplateMutationResult> CreateDraftForTeacherAsync(
        string teacherId,
        CreateTestTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<TestTemplateMutationResult> UpdateDraftSetupForTeacherAsync(
        Guid templateId,
        string teacherId,
        UpdateTestTemplateRequest request,
        CancellationToken cancellationToken = default);
}
