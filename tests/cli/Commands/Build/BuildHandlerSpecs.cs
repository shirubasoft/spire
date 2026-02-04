using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Spectre.Console.Testing;

using Spire.Cli.Commands.Build;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Commands.Build;

/// <summary>
/// Tests for successful image build scenarios.
/// </summary>
public class ValidResourceBuildSpecs
{
    [Test]
    public async Task Execute_WithValidIds_BuildsImages()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/app", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        containerService.TagExistsAsync("docker.io", "my-service", "abc1234", Arg.Any<CancellationToken>())
            .Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await containerService.Received(1).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for resource not found scenarios.
/// </summary>
public class ResourceNotFoundSpecs
{
    [Test]
    public async Task Execute_WhenResourceNotFound_ReturnsError()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = GlobalSharedResources.Empty;

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["unknown-id"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Resource not found");
    }
}

/// <summary>
/// Tests for resources without container settings.
/// </summary>
public class NoContainerSettingsSpecs
{
    [Test]
    public async Task Execute_WhenNoContainerSettings_ReturnsError()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Project,
                    ContainerMode = null,
                    ProjectMode = new ProjectModeSettings
                    {
                        ProjectPath = "/app/app.csproj"
                    },
                    GitRepository = null
                }
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("does not have container mode settings");
    }
}

/// <summary>
/// Tests for resources without a build command.
/// </summary>
public class NoBuildCommandSpecs
{
    [Test]
    public async Task Execute_WhenNoBuildCommand_ReturnsError()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "", // Empty build command
                        BuildWorkingDirectory = "/app"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("does not have a build command");
    }
}

/// <summary>
/// Tests for skipping builds when commit tag exists.
/// </summary>
public class ExistingCommitTagSpecs
{
    [Test]
    public async Task Execute_WhenCommitTagExists_Skips()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/app", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        // Commit tag already exists
        containerService.TagExistsAsync("docker.io", "my-service", "abc1234", Arg.Any<CancellationToken>())
            .Returns(true);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("skipping");
        await containerService.DidNotReceive().BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for force rebuild when commit tag exists.
/// </summary>
public class ForceRebuildSpecs
{
    [Test]
    public async Task Execute_WhenCommitTagExistsWithForce_Rebuilds()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/app", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        // Commit tag already exists
        containerService.TagExistsAsync("docker.io", "my-service", "abc1234", Arg.Any<CancellationToken>())
            .Returns(true);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: true, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("--force");
        await containerService.Received(1).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests that branch tag existence does not skip the build.
/// </summary>
public class ExistingBranchTagSpecs
{
    [Test]
    public async Task Execute_WhenBranchTagExists_StillBuilds()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/app", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        // Commit tag does NOT exist (branch tag existence doesn't matter)
        containerService.TagExistsAsync("docker.io", "my-service", "abc1234", Arg.Any<CancellationToken>())
            .Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await containerService.Received(1).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for handling build failures.
/// </summary>
public class BuildFailureSpecs
{
    [Test]
    public async Task Execute_WhenBuildFails_ReturnsError()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/app", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        containerService.TagExistsAsync("docker.io", "my-service", "abc1234", Arg.Any<CancellationToken>())
            .Returns(false);

        containerService.BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Build failed"));

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["my-service"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Build failed");
    }
}

/// <summary>
/// Tests for multiple resource builds.
/// </summary>
public class MultipleResourceBuildSpecs
{
    [Test]
    public async Task Execute_WithMultipleIds_BuildsAll()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["service-a"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "service-a",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-a"
                    },
                    ProjectMode = null,
                    GitRepository = null
                },
                ["service-b"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "service-b",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-b"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        containerService.TagExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(["service-a", "service-b"], force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await containerService.Received(2).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for auto-resolving resources from repository settings.
/// </summary>
public class BuildAllFromRepoSpecs
{
    [Test]
    public async Task Execute_WithNoIdsInRepo_BuildsRepoResources()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["svc-a"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "svc-a",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-a"
                    },
                    ProjectMode = null,
                    GitRepository = null
                },
                ["svc-b"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "svc-b",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-b"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        var repoResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["svc-a"] = new SharedResource { Mode = Mode.Container },
                ["svc-b"] = new SharedResource { Mode = Mode.Container }
            }
        };

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");

        repoReader.ReadAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns(repoResources);

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        containerService.TagExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(null, force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("repository");
        await containerService.Received(2).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for auto-resolving resources from global config with --global flag.
/// </summary>
public class BuildAllFromGlobalSpecs
{
    [Test]
    public async Task Execute_WithGlobalFlag_BuildsGlobalResources()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["svc-a"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "svc-a",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-a"
                    },
                    ProjectMode = null,
                    GitRepository = null
                },
                ["svc-b"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "svc-b",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-b"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        containerService.TagExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(null, force: false, global: true, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("global");
        await containerService.Received(2).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests for not being in a repository without --global flag.
/// </summary>
public class BuildNotInRepoWithoutGlobalSpecs
{
    [Test]
    public async Task Execute_WhenNotInRepoAndNoGlobal_PrintsMessage()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = GlobalSharedResources.Empty;

        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(null, force: false, global: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Not in a repository");
        await containerService.DidNotReceive().BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Tests that auto-resolve with --global skips resources without ContainerMode.
/// </summary>
public class BuildAllSkipsNonBuildableSpecs
{
    [Test]
    public async Task Execute_WithGlobal_SkipsResourcesWithoutContainerMode()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var tagGenerator = Substitute.For<IImageTagGenerator>();
        var repoReader = Substitute.For<IRepositorySharedResourcesReader>();

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["svc-a"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "svc-a",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish",
                        BuildWorkingDirectory = "/app-a"
                    },
                    ProjectMode = null,
                    GitRepository = null
                },
                ["svc-b"] = new SharedResource
                {
                    Mode = Mode.Project,
                    ContainerMode = null,
                    ProjectMode = new ProjectModeSettings
                    {
                        ProjectPath = "/app-b/app-b.csproj"
                    },
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
                CurrentBranch = "main",
                LatestCommitHash = "abc1234def",
                IsDirty = false
            });

        tagGenerator.Generate(Arg.Any<GitRepository>())
            .Returns(new ImageTags
            {
                CommitTag = "abc1234",
                BranchTag = "main"
            });

        containerService.TagExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);

        var handler = new BuildHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            repoReader,
            globalReader);

        var result = await handler.ExecuteAsync(null, force: false, global: true, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        // Only svc-a should be built (svc-b has no ContainerMode)
        await containerService.Received(1).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}
