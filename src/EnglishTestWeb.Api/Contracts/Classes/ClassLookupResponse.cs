namespace EnglishTestWeb.Api.Contracts.Classes;

public sealed record ClassLookupResponse(
    Guid ClassId,
    string ClassName,
    string ClassCode,
    string TeacherDisplayName,
    string Status);
