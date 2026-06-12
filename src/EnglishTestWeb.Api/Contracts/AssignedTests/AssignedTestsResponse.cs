namespace EnglishTestWeb.Api.Contracts.AssignedTests;

public sealed record AssignedTestsResponse(IReadOnlyList<AssignedTestItem> Items);
