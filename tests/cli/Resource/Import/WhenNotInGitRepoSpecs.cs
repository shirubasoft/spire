using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Import;

/// <summary>
/// Tests for resource import when not in a git repository.
/// </summary>
public class WhenNotInGitRepoSpecs
{
    [Test]
    public async Task Execute_WhenNotInGitRepo_ReturnsError()
    {
        // Arrange
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();
        var writer = Substitute.For<ISharedResourcesWriter>();

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(GlobalSharedResources.Empty);

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer, globalReader);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Not in a git repository");
    }
}
