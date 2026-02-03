using NSubstitute;

using Spire.Cli.Services;

namespace Spire.Cli.Tests;

/// <summary>
/// Tests for image tag generation returning three tags.
/// </summary>
public class ThreeTagsGenerationSpecs
{
    [Test]
    public async Task Generate_ReturnsThreeTags()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc1234def5678",
            IsDirty = false
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.All.Count()).IsEqualTo(3);
    }
}

/// <summary>
/// Tests for commit hash tag generation.
/// </summary>
public class CommitTagSpecs
{
    [Test]
    public async Task Generate_CommitIsShortHash()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc1234def5678",
            IsDirty = false
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.CommitTag).IsEqualTo("abc1234");
        await Assert.That(tags.CommitTag.Length).IsEqualTo(7);
    }

    [Test]
    public async Task Generate_WhenHashShorterThan7_UsesFullHash()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc12",
            IsDirty = false
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.CommitTag).IsEqualTo("abc12");
    }

    [Test]
    public async Task Generate_WhenDirty_AppendsDirtySuffix()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc1234def5678",
            IsDirty = true
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.CommitTag).IsEqualTo("abc1234-dirty");
    }
}

/// <summary>
/// Tests for branch tag sanitization.
/// </summary>
public class BranchTagSpecs
{
    [Test]
    public async Task Generate_BranchIsSanitized()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("feature/auth").Returns("feature-auth");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "feature/auth",
            LatestCommitHash = "abc1234def5678",
            IsDirty = false
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.BranchTag).IsEqualTo("feature-auth");
        sanitizer.Received(1).Sanitize("feature/auth");
    }

    [Test]
    public async Task Generate_WhenDirty_BranchTagHasDirtySuffix()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc1234def5678",
            IsDirty = true
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.BranchTag).IsEqualTo("main-dirty");
    }
}

/// <summary>
/// Tests for the "latest" tag.
/// </summary>
public class LatestTagSpecs
{
    [Test]
    public async Task Generate_LatestIsLiteral()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc1234def5678",
            IsDirty = false
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.LatestTag).IsEqualTo("latest");
    }

    [Test]
    public async Task Generate_LatestTag_IncludedInAll()
    {
        var sanitizer = Substitute.For<IBranchNameSanitizer>();
        sanitizer.Sanitize("main").Returns("main");
        var generator = new ImageTagGenerator(sanitizer);
        var repository = new GitRepository
        {
            RootPath = "/test/repo",
            CurrentBranch = "main",
            LatestCommitHash = "abc1234def5678",
            IsDirty = false
        };

        var tags = generator.Generate(repository);

        await Assert.That(tags.All).Contains("latest");
    }
}