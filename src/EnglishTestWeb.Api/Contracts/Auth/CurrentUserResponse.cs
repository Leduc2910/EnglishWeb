using System.Text.Json.Serialization;

namespace EnglishTestWeb.Api.Contracts.Auth;

public sealed record CurrentUserResponse(
    string UserId,
    string? Email,
    string? UserName,
    IReadOnlyList<string> Roles,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ActiveClassResponse? ActiveClass = null);
