using EnglishTestWeb.Api.Contracts.TestTemplates;
using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Application.TestTemplates;

public interface ITestTemplateMaterialService
{
    Task<TestMaterialAccessResult> ListMaterialsAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<TestMaterialMutationResult> UploadMaterialAsync(
        Guid templateId,
        string teacherId,
        string role,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<TestMaterialMutationResult> RemoveMaterialAsync(
        Guid templateId,
        string teacherId,
        Guid materialId,
        CancellationToken cancellationToken = default);
}

public sealed record TestMaterialAccessResult(
    bool Allowed,
    TestMaterialListResponse? Response,
    string? ErrorCode,
    int StatusCode = StatusCodes.Status200OK);

public sealed record TestMaterialMutationResult(
    bool Succeeded,
    TestMaterialResponse? Response,
    string? ErrorCode,
    int StatusCode);
