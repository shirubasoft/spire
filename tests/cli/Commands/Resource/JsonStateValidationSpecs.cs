using System.Text.Json;
using NSubstitute;
using Spectre.Console.Testing;
using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;
using Spire.Cli.Services.Git;

namespace Spire.Cli.Tests.Commands.Resource;

/// <summary>
/// Tests that verify JSON state before and after remove operations.
/// </summary>
public sealed class RemoveJsonStateValidationSpecs
{
    [Test]
    public async Task Remove_GlobalConfig_BeforeStateHasResource()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act & Assert - verify resource exists before
        await Assert.That(resources.ContainsResource("postgres")).IsTrue();

        var json = JsonSerializer.Serialize(resources);
        await Assert.That(json).Contains("postgres");
    }

    [Test]
    public async Task Remove_GlobalConfig_AfterStateWithoutResource()
    {
        // Arrange
        var resources = new GlobalSharedResources
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
        await Assert.That(result.ContainsResource("postgres")).IsFalse();

        var json = JsonSerializer.Serialize(result);
        await Assert.That(json).DoesNotContain("\"postgres\"");
    }

    [Test]
    public async Task Remove_GlobalConfig_OtherResourcesUnchanged()
    {
        // Arrange
        var redisResource = CreateResource();
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = redisResource
            }
        };

        var jsonBefore = JsonSerializer.Serialize(resources.Resources["redis"]);

        // Act
        var result = resources.RemoveResource("postgres");

        // Assert
        var jsonAfter = JsonSerializer.Serialize(result.Resources["redis"]);
        await Assert.That(jsonAfter).IsEqualTo(jsonBefore);
    }

    [Test]
    public async Task Remove_GlobalConfig_JsonStructureRemainsValid()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
            }
        };

        // Act
        var result = resources.RemoveResource("postgres");
        var json = JsonSerializer.Serialize(result);

        // Assert - should be valid JSON that can be deserialized
        var deserialized = JsonSerializer.Deserialize<GlobalSharedResources>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Resources).ContainsKey("redis");
    }

    [Test]
    public async Task Remove_RepositorySettings_BeforeStateHasResource()
    {
        // Arrange
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Assert
        await Assert.That(resources.ContainsResource("postgres")).IsTrue();
    }

    [Test]
    public async Task Remove_RepositorySettings_AfterStateWithoutResource()
    {
        // Arrange
        var resources = new RepositorySharedResources
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
        await Assert.That(result.ContainsResource("postgres")).IsFalse();
    }

    [Test]
    public async Task Remove_RepositorySettings_OtherResourcesUnchanged()
    {
        // Arrange
        var redisResource = CreateResource();
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = redisResource
            }
        };

        var jsonBefore = JsonSerializer.Serialize(resources.Resources["redis"]);

        // Act
        var result = resources.RemoveResource("postgres");

        // Assert
        var jsonAfter = JsonSerializer.Serialize(result.Resources["redis"]);
        await Assert.That(jsonAfter).IsEqualTo(jsonBefore);
    }

    [Test]
    public async Task Remove_RepositorySettings_JsonStructureRemainsValid()
    {
        // Arrange
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
            }
        };

        // Act
        var result = resources.RemoveResource("postgres");
        var json = JsonSerializer.Serialize(result);

        // Assert
        var deserialized = JsonSerializer.Deserialize<RepositorySharedResources>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Resources).ContainsKey("redis");
    }

    [Test]
    public async Task Remove_LastResource_ResultsInEmptyResourcesObject()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act
        var result = resources.RemoveResource("postgres");
        var json = JsonSerializer.Serialize(result);

        // Assert - should have empty resources object, not null
        await Assert.That(result.Resources).IsNotNull();
        await Assert.That(result.Resources).Count().IsEqualTo(0);
        await Assert.That(json).Contains("\"Resources\"");
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

/// <summary>
/// Tests that verify JSON state before and after clear operations.
/// </summary>
public sealed class ClearJsonStateValidationSpecs
{
    [Test]
    public async Task ClearAll_GlobalConfig_BeforeStateHasResources()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
            }
        };

        // Assert
        await Assert.That(resources.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ClearAll_GlobalConfig_AfterStateIsEmpty()
    {
        // Arrange
        var resources = new GlobalSharedResources
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
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearAll_GlobalConfig_JsonStructureRemainsValid()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act
        var result = resources.ClearResources();
        var json = JsonSerializer.Serialize(result);

        // Assert
        var deserialized = JsonSerializer.Deserialize<GlobalSharedResources>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Resources).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ClearSpecific_GlobalConfig_BeforeStateHasAllResources()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = CreateResource()
            }
        };

        // Assert
        await Assert.That(resources.ContainsResource("postgres")).IsTrue();
        await Assert.That(resources.ContainsResource("redis")).IsTrue();
        await Assert.That(resources.ContainsResource("mongo")).IsTrue();
    }

    [Test]
    public async Task ClearSpecific_GlobalConfig_AfterStateWithoutCleared()
    {
        // Arrange
        var resources = new GlobalSharedResources
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
        await Assert.That(result.ContainsResource("postgres")).IsFalse();
        await Assert.That(result.ContainsResource("redis")).IsFalse();
        await Assert.That(result.ContainsResource("mongo")).IsTrue();
    }

    [Test]
    public async Task ClearSpecific_GlobalConfig_UnspecifiedResourcesUnchanged()
    {
        // Arrange
        var mongoResource = CreateResource();
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = mongoResource
            }
        };

        var jsonBefore = JsonSerializer.Serialize(resources.Resources["mongo"]);

        // Act
        var result = resources.ClearResources(["postgres", "redis"]);

        // Assert
        var jsonAfter = JsonSerializer.Serialize(result.Resources["mongo"]);
        await Assert.That(jsonAfter).IsEqualTo(jsonBefore);
    }

    [Test]
    public async Task ClearAll_RepositorySettings_BeforeStateHasResources()
    {
        // Arrange
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource()
            }
        };

        // Assert
        await Assert.That(resources.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ClearAll_RepositorySettings_AfterStateIsEmpty()
    {
        // Arrange
        var resources = new RepositorySharedResources
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
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearAll_RepositorySettings_JsonStructureRemainsValid()
    {
        // Arrange
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act
        var result = resources.ClearResources();
        var json = JsonSerializer.Serialize(result);

        // Assert
        var deserialized = JsonSerializer.Deserialize<RepositorySharedResources>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Resources).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ClearSpecific_RepositorySettings_UnspecifiedResourcesUnchanged()
    {
        // Arrange
        var mongoResource = CreateResource();
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource(),
                ["redis"] = CreateResource(),
                ["mongo"] = mongoResource
            }
        };

        var jsonBefore = JsonSerializer.Serialize(resources.Resources["mongo"]);

        // Act
        var result = resources.ClearResources(["postgres", "redis"]);

        // Assert
        var jsonAfter = JsonSerializer.Serialize(result.Resources["mongo"]);
        await Assert.That(jsonAfter).IsEqualTo(jsonBefore);
    }

    [Test]
    public async Task Clear_ResultsInEmptyResourcesObject_NotNull()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = CreateResource()
            }
        };

        // Act
        var result = resources.ClearResources();
        var json = JsonSerializer.Serialize(result);

        // Assert
        await Assert.That(result.Resources).IsNotNull();
        await Assert.That(json).Contains("\"Resources\"");
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
