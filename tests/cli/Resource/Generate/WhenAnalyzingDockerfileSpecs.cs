using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Analysis;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Generate;

/// <summary>
/// Tests for generating resources from Dockerfiles.
/// </summary>
public class WhenAnalyzingDockerfileSpecs
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "spire-dockerfile-tests", Guid.NewGuid().ToString());
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
    public async Task Execute_WithDockerfilePath_ConfiguresContainerOnly()
    {
        // Arrange - create a real Dockerfile
        var dockerDir = Path.Combine(_tempDir, "docker", "postgres");
        Directory.CreateDirectory(dockerDir);
        var dockerfilePath = Path.Combine(dockerDir, "Dockerfile");
        await File.WriteAllTextAsync(dockerfilePath, "FROM postgres:latest");

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
            path: dockerDir,
            id: "postgres-db",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedGlobal).IsNotNull();
        await Assert.That(capturedGlobal!.Resources["postgres-db"].Mode).IsEqualTo(Mode.Container);
        await Assert.That(capturedGlobal.Resources["postgres-db"].ProjectMode).IsNull();
        await Assert.That(capturedGlobal.Resources["postgres-db"].ContainerMode).IsNotNull();
    }
}
