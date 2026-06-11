using EnglishTestWeb.Api.Domain.TestTemplates;

namespace EnglishTestWeb.Api.Application.TestTemplates;

public static class MaterialUploadValidation
{
    public const long PdfMaxBytes = 25L * 1024 * 1024;

    public const long AudioMaxBytes = 50L * 1024 * 1024;

    public static string? ValidateRoleForSkill(string skill, string role)
    {
        var normalizedSkill = skill.Trim().ToLowerInvariant();
        var normalizedRole = role.Trim().ToLowerInvariant();

        return normalizedSkill switch
        {
            TemplateSkill.Reading when normalizedRole == MaterialRoles.Pdf => null,
            TemplateSkill.Listening when normalizedRole is MaterialRoles.Pdf or MaterialRoles.Audio => null,
            TemplateSkill.Speaking when normalizedRole == MaterialRoles.Cue => null,
            _ => "materials.roleInvalid"
        };
    }

    public static string? ValidateFile(string role, string fileName, string contentType, long sizeBytes)
    {
        var normalizedRole = role.Trim().ToLowerInvariant();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var normalizedContentType = contentType.Trim().ToLowerInvariant();

        if (normalizedRole is MaterialRoles.Pdf or MaterialRoles.Cue)
        {
            if (!IsAllowedPdfFile(extension, normalizedContentType))
            {
                return "files.invalidType";
            }

            if (sizeBytes > PdfMaxBytes)
            {
                return "files.tooLarge";
            }

            return null;
        }

        if (normalizedRole == MaterialRoles.Audio)
        {
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3",
                ".m4a",
                ".wav"
            };

            var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "audio/mpeg",
                "audio/x-mpeg",
                "audio/mp4",
                "audio/x-m4a",
                "audio/wav",
                "audio/x-wav"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return "files.invalidType";
            }

            if (!string.IsNullOrEmpty(normalizedContentType)
                && normalizedContentType != "application/octet-stream"
                && !allowedContentTypes.Contains(normalizedContentType))
            {
                return "files.invalidType";
            }

            if (sizeBytes > AudioMaxBytes)
            {
                return "files.tooLarge";
            }

            return null;
        }

        return "materials.roleInvalid";
    }

    private static bool IsAllowedPdfFile(string extension, string contentType)
    {
        if (!string.Equals(extension, ".pdf", StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrEmpty(contentType))
        {
            return true;
        }

        return contentType is "application/pdf" or "application/octet-stream" or "application/x-pdf";
    }
}
