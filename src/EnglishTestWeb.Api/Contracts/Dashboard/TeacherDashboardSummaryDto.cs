namespace EnglishTestWeb.Api.Contracts.Dashboard;

public sealed record TeacherDashboardSummaryDto(
    int TemplateCount,
    int ActiveHomeworkCount,
    int OpenLiveExamCount,
    int RecentSubmissionCount,
    int PendingSpeakingCount);
