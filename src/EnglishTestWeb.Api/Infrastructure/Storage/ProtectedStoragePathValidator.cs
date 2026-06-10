namespace EnglishTestWeb.Api.Infrastructure.Storage;

public static class ProtectedStoragePathValidator
{
    public static string ValidateAndNormalize(string? rootPath, string contentRootPath, string? webRootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("ProtectedStorage:RootPath is required.");
        }

        var normalizedRoot = Normalize(rootPath);
        var effectiveWebRoot = string.IsNullOrWhiteSpace(webRootPath)
            ? Path.Combine(contentRootPath, "wwwroot")
            : webRootPath;

        if (IsSameOrUnder(normalizedRoot, Normalize(effectiveWebRoot)))
        {
            throw new InvalidOperationException("Protected storage root must not be under wwwroot.");
        }

        var repositoryRoot = FindRepositoryRoot(contentRootPath);
        var boundaryRoot = repositoryRoot ?? Normalize(contentRootPath);
        if (IsSameOrUnder(normalizedRoot, boundaryRoot))
        {
            throw new InvalidOperationException(
                repositoryRoot is null
                    ? "Protected storage root must live outside the application deployment directory."
                    : "Protected storage root must live outside the repository.");
        }

        return normalizedRoot;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(Normalize(startPath));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json"))
                || File.Exists(Path.Combine(directory.FullName, "EnglishTestWeb.sln"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return Normalize(directory.FullName);
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsSameOrUnder(string candidatePath, string parentPath)
    {
        var candidate = Normalize(candidatePath);
        var parent = Normalize(parentPath);

        return string.Equals(candidate, parent, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(parent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
