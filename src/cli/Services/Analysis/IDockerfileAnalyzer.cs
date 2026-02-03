namespace Spire.Cli.Services.Analysis;

/// <summary>
/// Analyzes directories containing Dockerfiles to extract container information for resource generation.
/// </summary>
public interface IDockerfileAnalyzer
{
    /// <summary>
    /// Analyzes a directory containing a Dockerfile.
    /// </summary>
    /// <param name="path">The path to the Dockerfile or directory containing a Dockerfile.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The analysis result, or null if no Dockerfile was found.</returns>
    Task<DockerfileAnalysisResult?> AnalyzeAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// The result of analyzing a Dockerfile.
/// </summary>
public sealed record DockerfileAnalysisResult
{
    /// <summary>
    /// The full path to the Dockerfile.
    /// </summary>
    public required string DockerfilePath { get; init; }

    /// <summary>
    /// The directory containing the Dockerfile.
    /// </summary>
    public required string BuildContext { get; init; }

    /// <summary>
    /// The suggested image name based on the directory name.
    /// </summary>
    public required string SuggestedImageName { get; init; }

    /// <summary>
    /// The command to build the container image.
    /// </summary>
    public string BuildCommand => $"docker build -t {SuggestedImageName} .";
}
