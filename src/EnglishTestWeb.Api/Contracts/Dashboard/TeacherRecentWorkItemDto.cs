namespace EnglishTestWeb.Api.Contracts.Dashboard;

public sealed record TeacherRecentWorkItemDto(
    string Type,
    string Id,
    string Title,
    string ClassName,
    string Mode,
    string Status,
    DateTimeOffset Timestamp);
