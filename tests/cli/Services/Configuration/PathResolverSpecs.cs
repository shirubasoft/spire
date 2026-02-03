using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.Services.Configuration;

/// <summary>
/// Tests for path resolution.
/// </summary>
public class PathResolverSpecs
{
    [Test]
    public async Task ToAbsolute_FromRepoRoot_ReturnsAbsolute()
    {
        // Arrange
        var relativePath = "./src/MyService";
        var repoRoot = "/test/repo";

        // Act
        var result = PathResolver.ToAbsolute(relativePath, repoRoot);

        // Assert
        await Assert.That(result).IsEqualTo("/test/repo/src/MyService");
    }

    [Test]
    public async Task ToAbsolute_WithDotDot_ResolvesCorrectly()
    {
        // Arrange
        var relativePath = "../shared/MyService";
        var repoRoot = "/test/repo";

        // Act
        var result = PathResolver.ToAbsolute(relativePath, repoRoot);

        // Assert
        await Assert.That(result).IsEqualTo("/test/shared/MyService");
    }

    [Test]
    public async Task ToAbsolute_AlreadyAbsolute_ReturnsUnchanged()
    {
        // Arrange
        var absolutePath = "/absolute/path/to/service";
        var repoRoot = "/test/repo";

        // Act
        var result = PathResolver.ToAbsolute(absolutePath, repoRoot);

        // Assert
        await Assert.That(result).IsEqualTo(absolutePath);
    }

    [Test]
    public async Task ToRelative_FromAbsolute_ReturnsRelative()
    {
        // Arrange
        var absolutePath = "/test/repo/src/MyService";
        var repoRoot = "/test/repo";

        // Act
        var result = PathResolver.ToRelative(absolutePath, repoRoot);

        // Assert
        await Assert.That(result).IsEqualTo("./src/MyService");
    }

    [Test]
    public async Task ToRelative_OutsideRepo_ReturnsOriginal()
    {
        // Arrange
        var absolutePath = "/other/path/MyService";
        var repoRoot = "/test/repo";

        // Act
        var result = PathResolver.ToRelative(absolutePath, repoRoot);

        // Assert
        await Assert.That(result).IsEqualTo(absolutePath);
    }
}
