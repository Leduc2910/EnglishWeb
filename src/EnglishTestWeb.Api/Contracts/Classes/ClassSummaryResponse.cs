namespace EnglishTestWeb.Api.Contracts.Classes;

public sealed record ClassSummaryResponse(
    Guid ClassId,
    string ClassName,
    string ClassCode,
    string Status,
    int EnrolledStudentCount);
