namespace Spire.Tests.RepositorySharedResourcesSpecs;

/// <summary>
/// Tests for RepositorySharedResources.ContainsResource method.
/// </summary>
public sealed class ContainsResourceSpecs
{
    [Test]
    public async Task ContainsResource_WhenExists_ReturnsTrue()
    {
        // Arrange
        var resources = new Spire.RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act
        var result = resources.ContainsResource("postgres");

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ContainsResource_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var resources = new Spire.RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act
        var result = resources.ContainsResource("nonexistent");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ContainsResource_OnEmptyResources_ReturnsFalse()
    {
        // Arrange
        var resources = Spire.RepositorySharedResources.Empty;

        // Act
        var result = resources.ContainsResource("any");

        // Assert
        await Assert.That(result).IsFalse();
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
