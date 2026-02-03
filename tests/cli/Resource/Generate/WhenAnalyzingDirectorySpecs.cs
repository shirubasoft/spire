using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Analysis;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Generate;

/// <summary>
/// Tests for generating resources from directories.
/// </summary>
public class WhenAnalyzingDirectorySpecs
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "spire-dir-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
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
    public async Task Execute_WithDirectoryContainingCsproj_AutoDetectsCsproj()
    {
        // Arrange - create directory with both .csproj and Dockerfile
        var projectDir = Path.Combine(_tempDir, "src", "MyService");
        Directory.CreateDirectory(projectDir);
        await File.WriteAllTextAsync(Path.Combine(projectDir, "MyService.csproj"), "<Project></Project>");
        await File.WriteAllTextAsync(Path.Combine(projectDir, "Dockerfile"), "FROM alpine");

        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var projectAnalyzer = new ProjectAnalyzer();
        var dockerfileAnalyzer = new DockerfileAnalyzer();
        var gitSettingsDetector = Substitute.For<IGitSettingsDetector>();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();

        gitSettingsDetector.DetectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitSettingsResult
            {
                RepositoryRoot = _tempDir,
                RemoteUrl = new Uri("https://github.com/user/my-app"),
                DefaultBranch = "main"
            });

        repositoryReader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = [] });

        GlobalSharedResources? capturedGlobal = null;
        await writer.SaveGlobalAsync(
            Arg.Do<GlobalSharedResources>(g => capturedGlobal = g),
            Arg.Any<CancellationToken>());

        var handler = new ResourceGenerateHandler(
            console, gitService, projectAnalyzer, dockerfileAnalyzer,
            gitSettingsDetector, writer, repositoryReader);

        // Act
        var result = await handler.ExecuteAsync(
            path: projectDir,
            id: "my-service",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert - Should prefer .csproj over Dockerfile
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedGlobal).IsNotNull();
        await Assert.That(capturedGlobal!.Resources["my-service"].Mode).IsEqualTo(Mode.Project);
        await Assert.That(capturedGlobal.Resources["my-service"].ProjectMode).IsNotNull();
    }

    [Test]
    public async Task Execute_WithDirectoryContainingDockerfile_AutoDetectsDockerfile()
    {
        // Arrange - create directory with only Dockerfile
        var dockerDir = Path.Combine(_tempDir, "docker", "postgres");
        Directory.CreateDirectory(dockerDir);
        await File.WriteAllTextAsync(Path.Combine(dockerDir, "Dockerfile"), "FROM postgres:latest");

        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var projectAnalyzer = new ProjectAnalyzer();
        var dockerfileAnalyzer = new DockerfileAnalyzer();
        var gitSettingsDetector = Substitute.For<IGitSettingsDetector>();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();

        gitSettingsDetector.DetectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitSettingsResult
            {
                RepositoryRoot = _tempDir,
                RemoteUrl = new Uri("https://github.com/user/my-app"),
                DefaultBranch = "main"
            });

        repositoryReader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = [] });

        var handler = new ResourceGenerateHandler(
            console, gitService, projectAnalyzer, dockerfileAnalyzer,
            gitSettingsDetector, writer, repositoryReader);

        // Act
        var result = await handler.ExecuteAsync(
            path: dockerDir,
            id: "postgres-db",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_WithEmptyDirectory_ReturnsError()
    {
        // Arrange - empty directory (already created in Setup)
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var projectAnalyzer = new ProjectAnalyzer();
        var dockerfileAnalyzer = new DockerfileAnalyzer();
        var gitSettingsDetector = Substitute.For<IGitSettingsDetector>();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();

        var handler = new ResourceGenerateHandler(
            console, gitService, projectAnalyzer, dockerfileAnalyzer,
            gitSettingsDetector, writer, repositoryReader);

        // Act
        var result = await handler.ExecuteAsync(
            path: emptyDir,
            id: "empty",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("No .csproj file or Dockerfile found");
    }
}
