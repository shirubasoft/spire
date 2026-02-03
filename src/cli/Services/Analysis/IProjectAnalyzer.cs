namespace Spire.Cli.Services.Analysis;

/// <summary>
/// Analyzes .csproj files to extract project information for resource generation.
/// </summary>
public interface IProjectAnalyzer
{
    /// <summary>
    /// Analyzes a .csproj file or directory containing a .csproj file.
    /// </summary>
    /// <param name="path">The path to the .csproj file or directory.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The analysis result, or null if no .csproj file was found.</returns>
    Task<ProjectAnalysisResult?> AnalyzeAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// The result of analyzing a .csproj file.
/// </summary>
public sealed record ProjectAnalysisResult
{
    /// <summary>
    /// The project name (without .csproj extension).
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// The full path to the .csproj file.
    /// </summary>
    public required string ProjectFilePath { get; init; }

    /// <summary>
    /// The project directory (directory containing the .csproj file).
    /// </summary>
    public required string ProjectDirectory { get; init; }

    /// <summary>
    /// The command to build the container image.
    /// </summary>
    public string BuildCommand => "dotnet publish --os linux --arch x64 /t:PublishContainer";
}
