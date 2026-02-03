namespace Spire.Tests.RepositorySharedResourcesSpecs;

/// <summary>
/// Tests for RepositorySharedResources.Count property.
/// </summary>
public sealed class CountSpecs
{
    [Test]
    public async Task Count_WithResources_ReturnsCount()
    {
        // Arrange
        var resources = new Spire.RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = CreateResource()
            }
        };

        // Act & Assert
        await Assert.That(resources.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Count_WhenEmpty_ReturnsZero()
    {
        // Arrange
        var resources = Spire.RepositorySharedResources.Empty;

        // Act & Assert
        await Assert.That(resources.Count).IsEqualTo(0);
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
