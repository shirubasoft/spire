using NSubstitute;

using Spire.Cli.Services.Analysis;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Services.Analysis;

/// <summary>
/// Tests for Git settings detector.
/// </summary>
public class GitSettingsDetectorSpecs
{
    [Test]
    public async Task Detect_InGitRepo_ReturnsSettings()
    {
        // Arrange
        var gitService = Substitute.For<IGitService>();
        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");
        gitService.GetRemoteUrlAsync("/test/repo", "origin", Arg.Any<CancellationToken>())
            .Returns("https://github.com/user/my-app.git");
        gitService.GetDefaultBranchAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns("main");

        var detector = new GitSettingsDetector(gitService);

        // Act
        var result = await detector.DetectAsync("/test/repo/src");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RepositoryRoot).IsEqualTo("/test/repo");
        await Assert.That(result.RemoteUrl.ToString()).IsEqualTo("https://github.com/user/my-app");
        await Assert.That(result.DefaultBranch).IsEqualTo("main");
    }

    [Test]
    public async Task Detect_NotInGitRepo_ReturnsNull()
    {
        // Arrange
        var gitService = Substitute.For<IGitService>();
        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var detector = new GitSettingsDetector(gitService);

        // Act
        var result = await detector.DetectAsync("/not/a/repo");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Detect_WithMultipleRemotes_UsesOrigin()
    {
        // Arrange
        var gitService = Substitute.For<IGitService>();
        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");
        gitService.GetRemoteUrlAsync("/test/repo", "origin", Arg.Any<CancellationToken>())
            .Returns("https://github.com/user/my-app.git");
        gitService.GetDefaultBranchAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns("main");

        var detector = new GitSettingsDetector(gitService);

        // Act
        var result = await detector.DetectAsync("/test/repo");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RemoteUrl.Host).IsEqualTo("github.com");
    }

    [Test]
    public async Task Detect_ParsesGitHubUrl_Correctly()
    {
        // Arrange
        var gitService = Substitute.For<IGitService>();
        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");
        gitService.GetRemoteUrlAsync("/test/repo", "origin", Arg.Any<CancellationToken>())
            .Returns("https://github.com/user/my-app.git");
        gitService.GetDefaultBranchAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns("main");

        var detector = new GitSettingsDetector(gitService);

        // Act
        var result = await detector.DetectAsync("/test/repo");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RemoteUrl.Host).IsEqualTo("github.com");
        await Assert.That(result.RemoteUrl.AbsolutePath).IsEqualTo("/user/my-app");
    }

    [Test]
    public async Task Detect_ParsesGitLabUrl_Correctly()
    {
        // Arrange
        var gitService = Substitute.For<IGitService>();
        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");
        gitService.GetRemoteUrlAsync("/test/repo", "origin", Arg.Any<CancellationToken>())
            .Returns("https://gitlab.com/group/project.git");
        gitService.GetDefaultBranchAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns("main");

        var detector = new GitSettingsDetector(gitService);

        // Act
        var result = await detector.DetectAsync("/test/repo");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RemoteUrl.Host).IsEqualTo("gitlab.com");
    }

    [Test]
    public async Task Detect_ParsesSshUrl_Correctly()
    {
        // Arrange
        var gitService = Substitute.For<IGitService>();
        gitService.GetRepositoryRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/test/repo");
        gitService.GetRemoteUrlAsync("/test/repo", "origin", Arg.Any<CancellationToken>())
            .Returns("git@github.com:user/my-app.git");
        gitService.GetDefaultBranchAsync("/test/repo", Arg.Any<CancellationToken>())
            .Returns("main");

        var detector = new GitSettingsDetector(gitService);

        // Act
        var result = await detector.DetectAsync("/test/repo");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RemoteUrl.Host).IsEqualTo("github.com");
        await Assert.That(result.RemoteUrl.AbsolutePath).IsEqualTo("/user/my-app");
    }
}
