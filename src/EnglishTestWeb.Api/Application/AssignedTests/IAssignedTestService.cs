using EnglishTestWeb.Api.Contracts.AssignedTests;

namespace EnglishTestWeb.Api.Application.AssignedTests;

public interface IAssignedTestService
{
    Task<IReadOnlyList<AssignedTestItem>> GetForStudentAsync(
        string studentId,
        Guid classId,
        CancellationToken cancellationToken = default);
}
