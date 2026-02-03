using Spire.Cli.Services.Analysis;

namespace Spire.Cli.Tests.Services.Analysis;

/// <summary>
/// Tests for Dockerfile analyzer.
/// </summary>
public class DockerfileAnalyzerSpecs
{
    private string _tempDir = null!;
    private DockerfileAnalyzer _analyzer = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dockerfile-analyzer-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _analyzer = new DockerfileAnalyzer();
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Analyze_DirectoryWithDockerfile_DetectsDockerfile()
    {
        // Arrange
        var dockerfilePath = Path.Combine(_tempDir, "Dockerfile");
        await File.WriteAllTextAsync(dockerfilePath, "FROM alpine:latest");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempDir);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DockerfilePath).IsEqualTo(dockerfilePath);
    }

    [Test]
    public async Task Analyze_DirectoryWithDockerfile_DeterminesBuildContext()
    {
        // Arrange
        var dockerfilePath = Path.Combine(_tempDir, "Dockerfile");
        await File.WriteAllTextAsync(dockerfilePath, "FROM alpine:latest");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempDir);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.BuildContext).IsEqualTo(_tempDir);
    }

    [Test]
    public async Task Analyze_DirectoryWithDockerfile_SuggestsImageName()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "my-service");
        Directory.CreateDirectory(subDir);
        var dockerfilePath = Path.Combine(subDir, "Dockerfile");
        await File.WriteAllTextAsync(dockerfilePath, "FROM alpine:latest");

        // Act
        var result = await _analyzer.AnalyzeAsync(subDir);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.SuggestedImageName).IsEqualTo("my-service");
    }

    [Test]
    public async Task Analyze_DirectoryWithoutDockerfile_ReturnsNull()
    {
        // Arrange - empty directory

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempDir);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Analyze_DockerfilePath_ReturnsResult()
    {
        // Arrange
        var dockerfilePath = Path.Combine(_tempDir, "Dockerfile");
        await File.WriteAllTextAsync(dockerfilePath, "FROM alpine:latest");

        // Act
        var result = await _analyzer.AnalyzeAsync(dockerfilePath);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DockerfilePath).IsEqualTo(dockerfilePath);
    }
}
