using NSubstitute;
using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource.Handlers;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Resource.Import;

/// <summary>
/// Tests for resource import when no settings file exists.
/// </summary>
public class WhenNoSettingsFileSpecs
{
    [Test]
    public async Task Execute_WhenNoSettingsFile_ReturnsError()
    {
        // Arrange
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var repositoryReader = Substitute.For<IRepositorySharedResourcesReader>();
        var writer = Substitute.For<ISharedResourcesWriter>();

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");

        repositoryReader.SettingsFileExists("/test/repo")
            .Returns(false);

        var handler = new ResourceImportHandler(console, gitService, repositoryReader, writer);

        // Act
        var result = await handler.ExecuteAsync(yes: true, force: false);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("No .aspire/settings.json file found");
    }
}
