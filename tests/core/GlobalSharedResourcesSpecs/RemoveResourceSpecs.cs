namespace Spire.Tests.GlobalSharedResourcesSpecs;

/// <summary>
/// Tests for GlobalSharedResources.RemoveResource method.
/// </summary>
public sealed class RemoveResourceSpecs
{
    [Test]
    public async Task RemoveResource_WhenExists_ReturnsNewInstanceWithout()
    {
        // Arrange
        var resources = new Spire.GlobalSharedResources
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
    public async Task RemoveResource_WhenNotExists_ThrowsKeyNotFoundException()
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
        await Assert.That(() => resources.RemoveResource("nonexistent"))
            .Throws<KeyNotFoundException>()
            .WithMessage("Resource 'nonexistent' not found.");
    }

    [Test]
    public async Task RemoveResource_PreservesOtherResources()
    {
        // Arrange
        var postgresResource = CreateResource();
        var redisResource = CreateResource();
        var mongoResource = CreateResource();

        var resources = new Spire.GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = postgresResource,
                ["redis"] = redisResource,
                ["mongo"] = mongoResource
            }
        };

        // Act
        var result = resources.RemoveResource("redis");

        // Assert
        await Assert.That(result.Resources).Count().IsEqualTo(2);
        await Assert.That(result.Resources["postgres"]).IsEqualTo(postgresResource);
        await Assert.That(result.Resources["mongo"]).IsEqualTo(mongoResource);
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
