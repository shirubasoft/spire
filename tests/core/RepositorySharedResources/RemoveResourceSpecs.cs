namespace Spire.Tests.RepositorySharedResources;

/// <summary>
/// Tests for RepositorySharedResources.RemoveResource method.
/// </summary>
public sealed class RemoveResourceSpecs
{
    [Test]
    public async Task RemoveResource_WhenExists_ReturnsNewInstanceWithout()
    {
        // Arrange
        var resources = new Spire.RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
            }
        };

        // Act
        var result = resources.RemoveResource("postgres");

        // Assert
        await Assert.That(result.Resources).DoesNotContainKey("postgres");
        await Assert.That(result.Resources).ContainsKey("redis");
        await Assert.That(result).IsNotEqualTo(resources);
    }

    [Test]
    public async Task RemoveResource_WhenNotExists_ReturnsUnchanged()
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
        var result = resources.RemoveResource("nonexistent");

        // Assert
        await Assert.That(result).IsEqualTo(resources);
    }

    [Test]
    public async Task RemoveResource_PreservesExternalResources()
    {
        // Arrange
        var externalResources = new List<ExternalResource>
        {
            new() { Url = "https://github.com/example/repo", Branch = "main" }
        };

        var resources = new Spire.RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
            },
            ExternalResources = externalResources
        };

        // Act
        var result = resources.RemoveResource("postgres");

        // Assert
        await Assert.That(result.ExternalResources).IsEquivalentTo(externalResources);
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
