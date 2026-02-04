using NSubstitute;
using Spectre.Console.Testing;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Commands.Resource;

/// <summary>
/// Tests for ResourceRemoveHandler when resource exists.
/// </summary>
public sealed class ResourceRemoveWhenResourceExistsSpecs
{
    [Test]
    public async Task Execute_WhenResourceExists_RemovesFromGlobal()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");

        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r => !r.ContainsResource("postgres")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WhenResourceExistsInRepo_RemovesFromBoth()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");

        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();

        var globalResources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        var repoResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(true);
        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc123",
                IsDirty = false
            });
        repoReader.ReadAsync("/repo", Arg.Any<CancellationToken>()).Returns(repoResources);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(globalResources);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r => !r.ContainsResource("postgres")),
            Arg.Any<CancellationToken>());
        await writer.Received(1).SaveRepositoryAsync(
            Arg.Is<RepositorySharedResources>(r => !r.ContainsResource("postgres")),
            "/repo",
            Arg.Any<CancellationToken>());
    }

    private static SharedResource CreateResource() => new()
    {
        Mode = Mode.Container,
        ContainerMode = new ContainerModeSettings
        {
            ImageName = "test",
            ImageRegistry = "localhost",
            ImageTag = "latest",
            BuildCommand = "docker build",
            BuildWorkingDirectory = "/app"
        },
        ProjectMode = null,
        GitRepository = null
    };
}

/// <summary>
/// Tests for ResourceRemoveHandler when resource does not exist.
/// </summary>
public sealed class ResourceRemoveWhenResourceNotFoundSpecs
{
    [Test]
    public async Task Execute_WhenResourceNotFound_ReturnsError()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();

        var resources = GlobalSharedResources.Empty;

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("nonexistent", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("not found");
        await writer.DidNotReceive().SaveGlobalAsync(
            Arg.Any<GlobalSharedResources>(),
            Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for ResourceRemoveHandler confirmation behavior.
/// </summary>
public sealed class ResourceRemoveConfirmationSpecs
{
    [Test]
    public async Task Execute_WithoutYes_PromptsConfirmation()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");

        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        await handler.ExecuteAsync("postgres", yes: false);

        // Assert
        await Assert.That(console.Output).Contains("Continue?");
    }

    [Test]
    public async Task Execute_WithYes_SkipsConfirmation()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).DoesNotContain("Continue?");
    }

    [Test]
    public async Task Execute_WhenUserDeclinesPrompt_DoesNotRemove()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("n");

        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceRemoveHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync("postgres", yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.DidNotReceive().SaveGlobalAsync(
            Arg.Any<GlobalSharedResources>(),
            Arg.Any<CancellationToken>());
    }

    private static SharedResource CreateResource() => new()
    {
        Mode = Mode.Container,
        ContainerMode = new ContainerModeSettings
        {
            ImageName = "test",
            ImageRegistry = "localhost",
            ImageTag = "latest",
            BuildCommand = "docker build",
            BuildWorkingDirectory = "/app"
        },
        ProjectMode = null,
        GitRepository = null
    };
}
