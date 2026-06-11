using EnglishTestWeb.Api.Contracts.TestTemplates;
using Microsoft.AspNetCore.Http;

namespace EnglishTestWeb.Api.Application.TestTemplates;

public interface IAnswerKeyService
{
    Task<AnswerKeyAccessResult> GetAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<AnswerKeyAccessResult> UpsertDraftAsync(
        Guid templateId,
        string teacherId,
        UpsertAnswerKeyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AnswerKeyAccessResult(
    bool Succeeded,
    AnswerKeyVersionResponse? Response,
    string? ErrorCode,
    int StatusCode = StatusCodes.Status200OK);
