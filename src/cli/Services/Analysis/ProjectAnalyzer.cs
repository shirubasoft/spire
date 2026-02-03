namespace Spire.Cli.Services.Analysis;

/// <summary>
/// Analyzes .csproj files to extract project information for resource generation.
/// </summary>
public sealed class ProjectAnalyzer : IProjectAnalyzer
{
    private const string CsprojExtension = ".csproj";

    /// <inheritdoc />
    public Task<ProjectAnalysisResult?> AnalyzeAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);

        // If it's a file path
        if (File.Exists(fullPath))
        {
            if (!fullPath.EndsWith(CsprojExtension, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ProjectAnalysisResult?>(null);
            }

            return Task.FromResult<ProjectAnalysisResult?>(CreateResult(fullPath));
        }

        // If it's a directory, look for a .csproj file
        if (Directory.Exists(fullPath))
        {
            var csprojFiles = Directory.GetFiles(fullPath, "*" + CsprojExtension, SearchOption.TopDirectoryOnly);

            if (csprojFiles.Length == 0)
            {
                return Task.FromResult<ProjectAnalysisResult?>(null);
            }

            // If multiple .csproj files exist, use the first one
            return Task.FromResult<ProjectAnalysisResult?>(CreateResult(csprojFiles[0]));
        }

        return Task.FromResult<ProjectAnalysisResult?>(null);
    }

    private static ProjectAnalysisResult CreateResult(string csprojPath)
    {
        var projectDirectory = Path.GetDirectoryName(csprojPath)!;
        var projectName = Path.GetFileNameWithoutExtension(csprojPath);

        return new ProjectAnalysisResult
        {
            ProjectName = projectName,
            ProjectFilePath = csprojPath,
            ProjectDirectory = projectDirectory
        };
    }
}
