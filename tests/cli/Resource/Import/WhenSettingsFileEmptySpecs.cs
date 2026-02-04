using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Import;

/// <summary>
/// Tests for resource import when settings file is empty.
/// </summary>
public class WhenSettingsFileEmptySpecs
{
    [Test]
    public async Task Execute_WhenSettingsFileEmpty_ImportsNothing()
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

        repositoryReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(new RepositorySharedResources { Resources = [] });

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(GlobalSharedResources.Empty);

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer, globalReader);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Imported: 0");
        await Assert.That(console.Output).Contains("Skipped (already exists): 0");
    }
}
