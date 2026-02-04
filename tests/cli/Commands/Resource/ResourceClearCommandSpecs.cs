using NSubstitute;
using Spectre.Console.Testing;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Commands.Resource;

/// <summary>
/// Tests for ResourceClearHandler clearing all resources.
/// </summary>
public sealed class ResourceClearAllSpecs
{
    [Test]
    public async Task Execute_WithoutIds_ClearsAllFromGlobal()
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
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = CreateResource()
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r => r.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WhenAlreadyEmpty_Succeeds()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();
        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var resources = GlobalSharedResources.Empty;

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
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
/// Tests for ResourceClearHandler clearing specific resources.
/// </summary>
public sealed class ResourceClearSpecificSpecs
{
    [Test]
    public async Task Execute_WithIds_ClearsOnlySpecified()
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
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = CreateResource()
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(["postgres", "redis"], includeRepo: false, yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r =>
                r.Count == 1 && r.ContainsResource("mongo")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WithInvalidId_ReturnsError()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();

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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(["nonexistent"], includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("not found");
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
/// Tests for ResourceClearHandler with --include-repo flag.
/// </summary>
public sealed class ResourceClearWithIncludeRepoSpecs
{
    [Test]
    public async Task Execute_WithIncludeRepo_ClearsBothLocations()
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
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: true, yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r => r.Count == 0),
            Arg.Any<CancellationToken>());
        await writer.Received(1).SaveRepositoryAsync(
            Arg.Is<RepositorySharedResources>(r => r.Count == 0),
            "/repo",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WithoutIncludeRepo_GlobalOnly()
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Any<GlobalSharedResources>(),
            Arg.Any<CancellationToken>());
        await writer.DidNotReceive().SaveRepositoryAsync(
            Arg.Any<RepositorySharedResources>(),
            Arg.Any<string>(),
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
/// Tests for ResourceClearHandler confirmation behavior.
/// </summary>
public sealed class ResourceClearConfirmationSpecs
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        await handler.ExecuteAsync(null, includeRepo: false, yes: false);

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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        await handler.ExecuteAsync(null, includeRepo: false, yes: true);

        // Assert
        await Assert.That(console.Output).DoesNotContain("Continue?");
    }

    [Test]
    public async Task Execute_WhenUserDeclinesPrompt_DoesNotClear()
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(null, includeRepo: false, yes: false);

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

/// <summary>
/// Tests for ResourceClearHandler clearing resources that exist only in repository settings.
/// </summary>
public sealed class ResourceClearRepoOnlyResourcesSpecs
{
    [Test]
    public async Task Execute_WithIdOnlyInRepo_WithIncludeRepo_ClearsFromRepo()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");

        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();

        // Global has no resources
        var globalResources = GlobalSharedResources.Empty;

        // Repo has a resource
        var repoResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["sample-web"] = CreateResource()
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act - clear resource that only exists in repo
        var result = await handler.ExecuteAsync(["sample-web"], includeRepo: true, yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveRepositoryAsync(
            Arg.Is<RepositorySharedResources>(r => r.Count == 0),
            "/repo",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WithIdOnlyInRepo_WithoutIncludeRepo_ReturnsError()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();

        // Global has no resources
        var globalResources = GlobalSharedResources.Empty;

        gitService.IsRepositoryCloned(Arg.Any<string>()).Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(globalResources);

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act - try to clear resource that doesn't exist in global
        var result = await handler.ExecuteAsync(["sample-web"], includeRepo: false, yes: true);

        // Assert - should fail because resource not found (not checking repo without --include-repo)
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("not found");
    }

    [Test]
    public async Task Execute_WithIdInBothLocations_ClearsBoth()
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
                ["my-service"] = CreateResource()
            }
        };

        var repoResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = CreateResource()
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act
        var result = await handler.ExecuteAsync(["my-service"], includeRepo: true, yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r => r.Count == 0),
            Arg.Any<CancellationToken>());
        await writer.Received(1).SaveRepositoryAsync(
            Arg.Is<RepositorySharedResources>(r => r.Count == 0),
            "/repo",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_WithMixedIds_ClearsFromAppropriateLocations()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");

        var writer = Substitute.For<ISharedResourcesWriter>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();
        var gitService = Substitute.For<IGitService>();

        // Global has one resource
        var globalResources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["global-only"] = CreateResource()
            }
        };

        // Repo has a different resource
        var repoResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["repo-only"] = CreateResource()
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

        var handler = new ResourceClearHandler(
            console,
            writer,
            repoReader,
            gitService,
            globalReader);

        // Act - clear both resources
        var result = await handler.ExecuteAsync(["global-only", "repo-only"], includeRepo: true, yes: false);

        // Assert
        await Assert.That(result).IsEqualTo(0);

        // Global should be cleared of global-only
        await writer.Received(1).SaveGlobalAsync(
            Arg.Is<GlobalSharedResources>(r => r.Count == 0),
            Arg.Any<CancellationToken>());

        // Repo should be cleared of repo-only
        await writer.Received(1).SaveRepositoryAsync(
            Arg.Is<RepositorySharedResources>(r => r.Count == 0),
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
