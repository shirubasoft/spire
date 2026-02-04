using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Analysis;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Generate;

/// <summary>
/// Tests for repository settings generation.
/// </summary>
public class RepositorySettingsGenerationSpecs
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "spire-repo-settings-tests", Guid.NewGuid().ToString());
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
    public async Task Generate_WritesRelativePathsToRepositorySettings()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "src", "MyService");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

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

        RepositorySharedResources? capturedRepo = null;
        await writer.SaveRepositoryAsync(
            Arg.Do<RepositorySharedResources>(r => capturedRepo = r),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        var handler = new ResourceGenerateHandler(
            console, gitService, projectAnalyzer, dockerfileAnalyzer,
            gitSettingsDetector, writer, repositoryReader);

        // Act
        var result = await handler.ExecuteAsync(
            path: csprojPath,
            id: "my-service",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedRepo).IsNotNull();
        await Assert.That(capturedRepo!.Resources["my-service"].ContainerMode!.BuildWorkingDirectory)
            .IsEqualTo("./src/MyService");
        await Assert.That(capturedRepo.Resources["my-service"].ProjectMode!.ProjectPath)
            .IsEqualTo("./src/MyService/MyService.csproj");
    }

    [Test]
    public async Task Generate_OmitsGitRepositoryFromRepositorySettings()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "src", "MyService");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

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

        RepositorySharedResources? capturedRepo = null;
        await writer.SaveRepositoryAsync(
            Arg.Do<RepositorySharedResources>(r => capturedRepo = r),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        var handler = new ResourceGenerateHandler(
            console, gitService, projectAnalyzer, dockerfileAnalyzer,
            gitSettingsDetector, writer, repositoryReader);

        // Act
        var result = await handler.ExecuteAsync(
            path: csprojPath,
            id: "my-service",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedRepo).IsNotNull();
        await Assert.That(capturedRepo!.Resources["my-service"].GitRepository).IsNull();
    }

    [Test]
    public async Task Generate_WritesAbsolutePathsToGlobalConfig()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "src", "MyService");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

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
            path: csprojPath,
            id: "my-service",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedGlobal).IsNotNull();
        await Assert.That(capturedGlobal!.Resources["my-service"].ContainerMode!.BuildWorkingDirectory)
            .IsEqualTo(projectDir);
        await Assert.That(capturedGlobal.Resources["my-service"].ProjectMode!.ProjectPath)
            .IsEqualTo(csprojPath);
    }

    [Test]
    public async Task Generate_IncludesGitRepositoryInGlobalConfig()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "src", "MyService");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

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
            path: csprojPath,
            id: "my-service",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedGlobal).IsNotNull();
        await Assert.That(capturedGlobal!.Resources["my-service"].GitRepository).IsNotNull();
        await Assert.That(capturedGlobal.Resources["my-service"].GitRepository!.Url.ToString())
            .IsEqualTo("https://github.com/user/my-app");
        await Assert.That(capturedGlobal.Resources["my-service"].GitRepository!.DefaultBranch)
            .IsEqualTo("main");
    }

    [Test]
    public async Task Generate_ProjectPath_EndsWithCsproj()
    {
        // Arrange
        var projectDir = Path.Combine(_tempDir, "src", "MyService");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "MyService.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

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

        RepositorySharedResources? capturedRepo = null;
        await writer.SaveRepositoryAsync(
            Arg.Do<RepositorySharedResources>(r => capturedRepo = r),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        var handler = new ResourceGenerateHandler(
            console, gitService, projectAnalyzer, dockerfileAnalyzer,
            gitSettingsDetector, writer, repositoryReader);

        // Act
        var result = await handler.ExecuteAsync(
            path: csprojPath,
            id: "my-service",
            imageName: null,
            imageRegistry: null,
            yes: true);

        // Assert - both global and repo project paths must end with .csproj
        await Assert.That(result).IsEqualTo(0);

        await Assert.That(capturedGlobal).IsNotNull();
        await Assert.That(capturedGlobal!.Resources["my-service"].ProjectMode!.ProjectPath)
            .EndsWith(".csproj");

        await Assert.That(capturedRepo).IsNotNull();
        await Assert.That(capturedRepo!.Resources["my-service"].ProjectMode!.ProjectPath)
            .EndsWith(".csproj");
    }
}
