namespace Spire.Cli.Services.Configuration;

/// <summary>
/// Resolves paths between relative and absolute forms for configuration files.
/// </summary>
public static class PathResolver
{
    /// <summary>
    /// Converts a relative path to an absolute path based on the repository root.
    /// </summary>
    /// <param name="relativePath">The relative path (e.g., "./src/MyService").</param>
    /// <param name="repositoryRoot">The repository root path.</param>
    /// <returns>The absolute path.</returns>
    public static string ToAbsolute(string relativePath, string repositoryRoot)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        // Handle ./ prefix
        var normalizedPath = relativePath.StartsWith("./", StringComparison.Ordinal)
            ? relativePath[2..]
            : relativePath;

        return Path.GetFullPath(Path.Combine(repositoryRoot, normalizedPath));
    }

    /// <summary>
    /// Converts an absolute path to a relative path based on the repository root.
    /// </summary>
    /// <param name="absolutePath">The absolute path.</param>
    /// <param name="repositoryRoot">The repository root path.</param>
    /// <returns>The relative path with "./" prefix.</returns>
    public static string ToRelative(string absolutePath, string repositoryRoot)
    {
        var fullAbsolutePath = Path.GetFullPath(absolutePath);
        var fullRepoRoot = Path.GetFullPath(repositoryRoot);

        if (!fullAbsolutePath.StartsWith(fullRepoRoot, StringComparison.OrdinalIgnoreCase))
        {
            // Path is outside repository, return as-is
            return absolutePath;
        }

        var relativePath = Path.GetRelativePath(fullRepoRoot, fullAbsolutePath);

        // Add "./" prefix for clarity
        if (!relativePath.StartsWith(".", StringComparison.Ordinal))
        {
            relativePath = "./" + relativePath;
        }

        // Normalize to forward slashes for cross-platform compatibility in JSON
        return relativePath.Replace('\\', '/');
    }
}
