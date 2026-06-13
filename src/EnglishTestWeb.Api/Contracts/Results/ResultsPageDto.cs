namespace EnglishTestWeb.Api.Contracts.Results;

public sealed record ResultsPageDto(
    IReadOnlyList<ResultRowDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int NeedsGrading);     // Speaking submissions có status="submitted" trong filtered set
