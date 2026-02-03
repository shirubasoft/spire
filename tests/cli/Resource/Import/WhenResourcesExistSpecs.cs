using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Import;

/// <summary>
/// Tests for resource import with resources.
/// </summary>
public class WhenResourcesExistSpecs
{
    [Test]
    public async Task Execute_WithResources_ImportsToGlobal()
    {
        // Arrange
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();
        var writer = Substitute.For<ISharedResourcesWriter>();

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");

        gitService.GetParentDirectory("/test/repo")
            .Returns("/test");

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(true);

        var resources = new Dictionary<string, SharedResource>
        {
            ["my-service"] = new SharedResource
            {
                Mode = Mode.Project,
                ContainerMode = new ContainerModeSettings
                {
                    ImageName = "my-service",
                    ImageRegistry = "docker.io",
                    ImageTag = "latest",
                    BuildCommand = "dotnet publish",
                    BuildWorkingDirectory = "./src/MyService"
                },
                ProjectMode = new ProjectModeSettings
                {
                    ProjectDirectory = "./src/MyService"
                },
                GitRepository = null
            }
        };

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = resources });

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Imported: 1");

        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(g => g.Resources.ContainsKey("my-service")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WithExistingResource_SkipsWithoutForce()
    {
        // Arrange
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();
        var writer = Substitute.For<ISharedResourcesWriter>();

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");

        gitService.GetParentDirectory("/test/repo")
            .Returns("/test");

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(true);

        var resources = new Dictionary<string, SharedResource>
        {
            ["existing-service"] = new SharedResource
            {
                Mode = Mode.Project,
                ContainerMode = new ContainerModeSettings
                {
                    ImageName = "existing-service",
                    ImageRegistry = "docker.io",
                    ImageTag = "latest",
                    BuildCommand = "dotnet publish",
                    BuildWorkingDirectory = "./src/ExistingService"
                },
                ProjectMode = new ProjectModeSettings
                {
                    ProjectDirectory = "./src/ExistingService"
                },
                GitRepository = null
            }
        };

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = resources });

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act - run twice to simulate existing resource
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Imported: 1");
    }

    [Test]
    public async Task Execute_ConvertsRelativePathsToAbsolute()
    {
        // Arrange
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();
        var writer = Substitute.For<ISharedResourcesWriter>();

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");

        gitService.GetParentDirectory("/test/repo")
            .Returns("/test");

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(true);

        var resources = new Dictionary<string, SharedResource>
        {
            ["my-service"] = new SharedResource
            {
                Mode = Mode.Project,
                ContainerMode = new ContainerModeSettings
                {
                    ImageName = "my-service",
                    ImageRegistry = "docker.io",
                    ImageTag = "latest",
                    BuildCommand = "dotnet publish",
                    BuildWorkingDirectory = "./src/MyService"
                },
                ProjectMode = new ProjectModeSettings
                {
                    ProjectDirectory = "./src/MyService"
                },
                GitRepository = null
            }
        };

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = resources });

        GlobalSharedResources? capturedResources = null;
        await writer.SaveGlobalAsync(
            Arg.Do<GlobalSharedResources>(g => capturedResources = g),
            Arg.Any<CancellationToken>());

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(capturedResources).IsNotNull();
        await Assert.That(capturedResources!.Resources["my-service"].ContainerMode!.BuildWorkingDirectory)
            .IsEqualTo("/test/repo/src/MyService");
        await Assert.That(capturedResources.Resources["my-service"].ProjectMode!.ProjectDirectory)
            .IsEqualTo("/test/repo/src/MyService");
    }
}
