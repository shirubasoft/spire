namespace Spire.Tests.GlobalSharedResources;

/// <summary>
/// Tests for GlobalSharedResources.ClearResources method.
/// </summary>
public sealed class ClearResourcesSpecs
{
    [Test]
    public async Task ClearResources_WithoutIds_ReturnsEmptyInstance()
    {
        // Arrange
        var resources = new Spire.GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = CreateResource()
            }
        };

        // Act
        var result = resources.ClearResources();

        // Assert
        await Assert.That(result.Resources).Count().IsEqualTo(0);
        await Assert.That(result).IsEqualTo(Spire.GlobalSharedResources.Empty);
    }

    [Test]
    public async Task ClearResources_WithIds_ReturnsWithoutSpecified()
    {
        // Arrange
        var resources = new Spire.GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = CreateResource()
            }
        };

        // Act
        var result = resources.ClearResources(["postgres", "redis"]);

        // Assert
        await Assert.That(result.Resources).Count().IsEqualTo(1);
        await Assert.That(result.Resources).ContainsKey("mongo");
        await Assert.That(result.Resources).DoesNotContainKey("postgres");
        await Assert.That(result.Resources).DoesNotContainKey("redis");
    }

    [Test]
    public async Task ClearResources_PreservesUnspecifiedResources()
    {
        // Arrange
        var mongoResource = CreateResource();
        var resources = new Spire.GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = mongoResource
            }
        };

        // Act
        var result = resources.ClearResources(["postgres", "redis"]);

        // Assert
        await Assert.That(result.Resources["mongo"]).IsEqualTo(mongoResource);
    }

    [Test]
    public async Task ClearResources_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var resources = new Spire.GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act & Assert
        await Assert.That(() => resources.ClearResources(["nonexistent"]))
            .Throws<KeyNotFoundException>()
            .WithMessage("Resource 'nonexistent' not found.");
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
