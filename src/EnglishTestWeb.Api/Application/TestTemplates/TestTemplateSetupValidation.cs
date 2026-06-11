using EnglishTestWeb.Api.Domain.TestTemplates;

namespace EnglishTestWeb.Api.Application.TestTemplates;

public static class TestTemplateSetupValidation
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 120;
    public const int MaxTagCount = 10;
    public const int MaxTagLength = 32;

    public static string? ValidateTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length < TitleMinLength)
        {
            return "templates.nameRequired";
        }

        if (trimmed.Length > TitleMaxLength)
        {
            return "templates.titleTooLong";
        }

        return null;
    }

    public static string? ValidateSkill(string? skill)
    {
        var normalized = skill?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "templates.skillRequired";
        }

        if (normalized is not (TemplateSkill.Reading or TemplateSkill.Listening or TemplateSkill.Speaking))
        {
            return "templates.skillInvalid";
        }

        return null;
    }

    public static string? ValidateDescription(string? description)
    {
        if (description is not null && description.Length > 2000)
        {
            return "templates.descriptionTooLong";
        }

        return null;
    }

    public static string? ValidateTags(IReadOnlyList<string>? tags, out IReadOnlyList<string> normalizedTags)
    {
        normalizedTags = NormalizeTags(tags);
        if (normalizedTags.Count > MaxTagCount)
        {
            return "templates.tagLimit";
        }

        if (normalizedTags.Any(tag => tag.Length > MaxTagLength))
        {
            return "templates.tagTooLong";
        }

        return null;
    }

    public static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return [];
        }

        var result = new List<string>();
        foreach (var tag in tags)
        {
            if (tag is null)
            {
                continue;
            }

            var trimmed = tag.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (result.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(trimmed);
        }

        return result;
    }
}
