using NSubstitute;

using Spectre.Console.Testing;

using Spire.Cli.Commands.Resource;
using Spire.Cli.Services;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests;

/// <summary>
/// Integration tests for dotnet project builds.
/// Note: These tests use mocks but test the full handler flow.
/// Real integration tests would require docker/podman to be installed.
/// </summary>
public class DotnetProjectBuildIntegrationSpecs
{
    [Test]
    public async Task Build_WithDotnetProject_RunsPublishCommand()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var branchSanitizer = new BranchNameSanitizer();
        var tagGenerator = new ImageTagGenerator(branchSanitizer);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-api"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-api",
                        ImageRegistry = "docker.io/myorg",
                        ImageTag = "latest",
                        BuildCommand = "dotnet publish --os linux --arch x64 /t:PublishContainer",
                        BuildWorkingDirectory = "/home/user/projects/my-api"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/home/user/projects/my-api", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
            CurrentBranch = "feature/add-auth",
                LatestCommitHash = "abc1234def5678",
                IsDirty = false
            });

        containerService.TagExistsAsync("docker.io/myorg", "my-api", "abc1234", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ResourceBuildImageHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            () => resources);

        var result = await handler.ExecuteAsync(["my-api"], force: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);

        // Verify build was called with correct command
        await containerService.Received(1).BuildImageAsync(
            Arg.Is<ContainerImageBuildRequest>(req =>
                req.Command == "dotnet publish --os linux --arch x64 /t:PublishContainer" &&
                req.WorkingDirectory == "/home/user/projects/my-api"),
            Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Integration tests for dockerfile builds.
/// </summary>
public class DockerfileBuildIntegrationSpecs
{
    [Test]
    public async Task Build_WithDockerfile_RunsDockerBuild()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var branchSanitizer = new BranchNameSanitizer();
        var tagGenerator = new ImageTagGenerator(branchSanitizer);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres-db"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "postgres-db",
                        ImageRegistry = "docker.io/myorg",
                        ImageTag = "latest",
                        BuildCommand = "docker build -t postgres-db .",
                        BuildWorkingDirectory = "/home/user/projects/postgres-custom"
                    },
                    ProjectMode = null,
                    GitRepository = null
                }
            }
        };

        gitService.GetRepositoryAsync("/home/user/projects/postgres-custom", Arg.Any<CancellationToken>())
            .Returns(new GitRepository
            {
                RootPath = "/test/repo",
            CurrentBranch = "main",
                LatestCommitHash = "def5678abc1234",
                IsDirty = false
            });

        containerService.TagExistsAsync("docker.io/myorg", "postgres-db", "def5678", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ResourceBuildImageHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            () => resources);

        var result = await handler.ExecuteAsync(["postgres-db"], force: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);

        // Verify build was called
        await containerService.Received(1).BuildImageAsync(
            Arg.Is<ContainerImageBuildRequest>(req =>
                req.Command == "docker build -t postgres-db ." &&
                req.WorkingDirectory == "/home/user/projects/postgres-custom"),
            Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Integration tests for skip logic with existing images.
/// </summary>
public class ImageExistsSkipIntegrationSpecs
{
    [Test]
    public async Task Build_WhenImageExists_Skips()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var branchSanitizer = new BranchNameSanitizer();
        var tagGenerator = new ImageTagGenerator(branchSanitizer);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-api"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-api",
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
                LatestCommitHash = "abc1234def5678",
                IsDirty = false
            });

        // Image with commit tag already exists
        containerService.TagExistsAsync("docker.io", "my-api", "abc1234", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new ResourceBuildImageHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            () => resources);

        var result = await handler.ExecuteAsync(["my-api"], force: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("skipping");

        // Build should not be called
        await containerService.DidNotReceive().BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Integration tests for force rebuild.
/// </summary>
public class ForceRebuildIntegrationSpecs
{
    [Test]
    public async Task Build_WithForce_Rebuilds()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var branchSanitizer = new BranchNameSanitizer();
        var tagGenerator = new ImageTagGenerator(branchSanitizer);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-api"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-api",
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
                LatestCommitHash = "abc1234def5678",
                IsDirty = false
            });

        // Image exists but we're using --force
        containerService.TagExistsAsync("docker.io", "my-api", "abc1234", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new ResourceBuildImageHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            () => resources);

        var result = await handler.ExecuteAsync(["my-api"], force: true, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("--force");

        // Build should be called despite existing image
        await containerService.Received(1).BuildImageAsync(Arg.Any<ContainerImageBuildRequest>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Integration tests for applying all three tags.
/// </summary>
public class AllTagsAppliedIntegrationSpecs
{
    [Test]
    public async Task Build_AppliesAllTags()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var branchSanitizer = new BranchNameSanitizer();
        var tagGenerator = new ImageTagGenerator(branchSanitizer);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-api"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-api",
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
            CurrentBranch = "feature/auth",
                LatestCommitHash = "abc1234def5678",
                IsDirty = false
            });

        containerService.TagExistsAsync("docker.io", "my-api", "abc1234", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ResourceBuildImageHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            () => resources);

        var result = await handler.ExecuteAsync(["my-api"], force: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);

        // Verify tagging was called with branch and latest tags
        await containerService.Received(1).TagImageAsync(
            "docker.io/my-api:abc1234",
            Arg.Is<IEnumerable<string>>(tags =>
                tags.Contains("feature-auth") && tags.Contains("latest")),
            Arg.Any<CancellationToken>());

        // Output should show all three tags were applied
        await Assert.That(console.Output).Contains("abc1234");
        await Assert.That(console.Output).Contains("feature-auth");
        await Assert.That(console.Output).Contains("latest");
    }
}

/// <summary>
/// Integration tests for dirty repository handling.
/// </summary>
public class DirtyRepositoryIntegrationSpecs
{
    [Test]
    public async Task Build_WhenDirty_AddsDirtySuffixToTags()
    {
        var console = new TestConsole();
        var gitService = Substitute.For<IGitService>();
        var containerService = Substitute.For<IContainerImageService>();
        var branchSanitizer = new BranchNameSanitizer();
        var tagGenerator = new ImageTagGenerator(branchSanitizer);

        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-api"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-api",
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
                LatestCommitHash = "abc1234def5678",
                IsDirty = true
            });

        containerService.TagExistsAsync("docker.io", "my-api", "abc1234-dirty", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ResourceBuildImageHandler(
            console,
            gitService,
            containerService,
            tagGenerator,
            () => resources);

        var result = await handler.ExecuteAsync(["my-api"], force: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);

        // Output should contain dirty suffix
        await Assert.That(console.Output).Contains("abc1234-dirty");
        await Assert.That(console.Output).Contains("main-dirty");
    }
}