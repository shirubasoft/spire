namespace Spire.Cli.Services.Analysis;

/// <summary>
/// Analyzes directories containing Dockerfiles to extract container information for resource generation.
/// </summary>
public sealed class DockerfileAnalyzer : IDockerfileAnalyzer
{
    private const string DockerfileName = "Dockerfile";

    /// <inheritdoc />
    public Task<DockerfileAnalysisResult?> AnalyzeAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);

        // If it's a file path pointing to a Dockerfile
        if (File.Exists(fullPath))
        {
            var fileName = Path.GetFileName(fullPath);
            if (!fileName.Equals(DockerfileName, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<DockerfileAnalysisResult?>(null);
            }

            var directory = Path.GetDirectoryName(fullPath)!;
            return Task.FromResult<DockerfileAnalysisResult?>(CreateResult(fullPath, directory));
        }

        // If it's a directory, look for a Dockerfile
        if (Directory.Exists(fullPath))
        {
            var dockerfilePath = Path.Combine(fullPath, DockerfileName);

            if (!File.Exists(dockerfilePath))
            {
                return Task.FromResult<DockerfileAnalysisResult?>(null);
            }

            return Task.FromResult<DockerfileAnalysisResult?>(CreateResult(dockerfilePath, fullPath));
        }

        return Task.FromResult<DockerfileAnalysisResult?>(null);
    }

    private static DockerfileAnalysisResult CreateResult(string dockerfilePath, string directory)
    {
        var directoryName = new DirectoryInfo(directory).Name;
        var imageName = directoryName.ToLowerInvariant().Replace(' ', '-');

        return new DockerfileAnalysisResult
        {
            DockerfilePath = dockerfilePath,
            BuildContext = directory,
            SuggestedImageName = imageName
        };
    }
}
