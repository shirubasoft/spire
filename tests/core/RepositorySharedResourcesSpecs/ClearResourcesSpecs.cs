namespace Spire.Tests.RepositorySharedResourcesSpecs;

/// <summary>
/// Tests for RepositorySharedResources.ClearResources method.
/// </summary>
public sealed class ClearResourcesSpecs
{
    [Test]
    public async Task ClearResources_WithoutIds_ClearsAllResources()
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
        var result = resources.ClearResources();

        // Assert
        await Assert.That(result.Resources).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ClearResources_WithIds_RemovesOnlySpecified()
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

        // Act
        var result = resources.ClearResources(["postgres", "redis"]);

        // Assert
        await Assert.That(result.Resources).Count().IsEqualTo(1);
        await Assert.That(result.Resources).ContainsKey("mongo");
    }

    [Test]
    public async Task ClearResources_PreservesExternalResources()
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
                ["postgres"] = CreateResource()
            },
            ExternalResources = externalResources
        };

        // Act
        var result = resources.ClearResources();

        // Assert
        await Assert.That(result.ExternalResources).IsEquivalentTo(externalResources);
    }

    [Test]
    public async Task ClearResources_WithNonexistentId_IgnoresIt()
    {
        // Arrange
        var resources = new Spire.RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act - should not throw
        var result = resources.ClearResources(["nonexistent"]);

        // Assert
        await Assert.That(result.Resources).ContainsKey("postgres");
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
