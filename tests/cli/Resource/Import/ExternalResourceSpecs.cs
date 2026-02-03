using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Import;

/// <summary>
/// Tests for importing external resources.
/// </summary>
public class ExternalResourceSpecs
{
    [Test]
    public async Task Import_WithExternalResource_ClonesRepo()
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

        gitService.IsRepositoryCloned("/test/shared-repo")
            .Returns(false);

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(true);

        var mainResources = new RepositorySharedResources
        {
            Resources = [],
            ExternalResources =
            [
                new ExternalResource { Url = "https://github.com/org/shared-repo" }
            ]
        };

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(mainResources);

        repositoryReader.ReadAsync("/test/shared-repo", Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = [] });

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await gitService.Received(1).CloneRepositoryAsync(
            "https://github.com/org/shared-repo",
            "/test/shared-repo",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Import_WithExternalResourceAlreadyCloned_SkipsClone()
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

        gitService.IsRepositoryCloned("/test/shared-repo")
            .Returns(true);

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(true);

        var mainResources = new RepositorySharedResources
        {
            Resources = [],
            ExternalResources =
            [
                new ExternalResource { Url = "https://github.com/org/shared-repo" }
            ]
        };

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(mainResources);

        repositoryReader.ReadAsync("/test/shared-repo", Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = [] });

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Already cloned");
        await gitService.DidNotReceive().CloneRepositoryAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Import_WithCircularExternalRef_DetectsAndStops()
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

        gitService.IsRepositoryCloned("/test/shared-repo")
            .Returns(true);

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(true);

        // Main repo references shared-repo
        var mainResources = new RepositorySharedResources
        {
            Resources = [],
            ExternalResources =
            [
                new ExternalResource { Url = "https://github.com/org/shared-repo" }
            ]
        };

        // Shared repo references main repo back (circular)
        var sharedResources = new RepositorySharedResources
        {
            Resources = [],
            ExternalResources =
            [
                new ExternalResource { Url = "https://github.com/org/repo" }
            ]
        };

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(mainResources);

        repositoryReader.ReadAsync("/test/shared-repo", Arg.Any<CancellationToken>())
            .Returns(sharedResources);

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        // Should complete without infinite loop
    }
}
