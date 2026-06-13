using EnglishTestWeb.Api.Contracts.Results;

namespace EnglishTestWeb.Api.Application.Results;

public interface IResultsService
{
    Task<ResultsPageDto> GetResultsForTeacherAsync(
        string teacherId,
        ResultsFilter filter,
        CancellationToken cancellationToken = default);
}
