namespace EnglishTestWeb.Api.Contracts.Classes;

public sealed record ClassStudentResponse(
    string StudentId,
    string DisplayName,
    string? Email,
    string MembershipStatus);
