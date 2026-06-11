using System.Text.Json;

namespace EnglishTestWeb.Api.Application.TestTemplates;

internal static class TestTemplateTagsSerializer
{
    internal const int MaxTagsJsonLength = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static IReadOnlyList<string> Deserialize(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(tagsJson, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal static string? Serialize(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return null;
        }

        var serialized = JsonSerializer.Serialize(tags, SerializerOptions);
        return serialized.Length > MaxTagsJsonLength ? null : serialized;
    }

    internal static string? ValidateSerializedLength(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return null;
        }

        var serialized = JsonSerializer.Serialize(tags, SerializerOptions);
        return serialized.Length > MaxTagsJsonLength ? "templates.tagsStorageLimit" : null;
    }
}
