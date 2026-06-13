using EnglishTestWeb.Api.Contracts.Dashboard;

namespace EnglishTestWeb.Api.Application.Dashboard;

public interface ITeacherDashboardService
{
    Task<TeacherDashboardDto> GetDashboardAsync(
        string teacherId,
        Guid? classId = null,
        CancellationToken cancellationToken = default);
}
