namespace EnglishTestWeb.Api.Contracts.Dashboard;

public sealed record TeacherDashboardDto(
    TeacherDashboardSummaryDto Summary,
    IReadOnlyList<TeacherRecentWorkItemDto> RecentWork);
