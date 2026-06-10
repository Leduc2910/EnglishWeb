namespace EnglishTestWeb.Api.Contracts.Classes;

public sealed record ClassDetailResponse(
    Guid ClassId,
    string ClassName,
    string ClassCode,
    string Status,
    IReadOnlyList<ClassStudentResponse> Students);
