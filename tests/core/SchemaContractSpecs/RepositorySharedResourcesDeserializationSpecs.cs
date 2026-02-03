using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Tests.SchemaContractSpecs;

/// <summary>
/// Tests that RepositorySharedResources deserializes correctly per the JSON schema.
/// </summary>
public class RepositorySharedResourcesDeserializationSpecs
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Test]
    public async Task Deserialize_WithEmptyResources_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "resources": {}
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize<RepositorySharedResources>(json, JsonOptions);

        // Assert
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Resources).IsEmpty();
    }

    [Test]
    public async Task Deserialize_WithMinimalResource_Succeeds()
    {
        // Arrange - resource with only required 'mode' field
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container"
                    }
                }
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize<RepositorySharedResources>(json, JsonOptions);

        // Assert
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Resources).ContainsKey("my-service");
        await Assert.That(config.Resources["my-service"].Mode).IsEqualTo(Mode.Container);
        await Assert.That(config.Resources["my-service"].ContainerMode).IsNull();
        await Assert.That(config.Resources["my-service"].ProjectMode).IsNull();
        await Assert.That(config.Resources["my-service"].GitRepository).IsNull();
    }

    [Test]
    public async Task Deserialize_WithFullResource_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "my-service": {
                        "mode": "container",
                        "containerMode": {
                            "imageName": "my-service",
                            "imageRegistry": "docker.io",
                            "imageTag": "latest",
                            "buildCommand": "docker build -t my-service .",
                            "buildWorkingDirectory": "./src/MyService"
                        },
                        "projectMode": {
                            "projectDirectory": "./src/MyService"
                        }
                    }
                }
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize<RepositorySharedResources>(json, JsonOptions);

        // Assert
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Resources["my-service"].ContainerMode).IsNotNull();
        await Assert.That(config.Resources["my-service"].ProjectMode).IsNotNull();
    }

    [Test]
    public async Task Deserialize_WithExternalResources_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "resources": {},
                "externalResources": [
                    {
                        "url": "https://github.com/org/external-repo",
                        "branch": "main"
                    }
                ]
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize<RepositorySharedResources>(json, JsonOptions);

        // Assert
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.ExternalResources.Count).IsEqualTo(1);
        await Assert.That(config.ExternalResources[0].Url.ToString()).IsEqualTo("https://github.com/org/external-repo");
    }

    [Test]
    public async Task Deserialize_WithMultipleResources_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "resources": {
                    "postgres": {
                        "mode": "container"
                    },
                    "redis": {
                        "mode": "container"
                    },
                    "api": {
                        "mode": "project",
                        "projectMode": {
                            "projectDirectory": "./src/Api"
                        }
                    }
                }
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize<RepositorySharedResources>(json, JsonOptions);

        // Assert
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Resources.Count).IsEqualTo(3);
        await Assert.That(config.Resources).ContainsKey("postgres");
        await Assert.That(config.Resources).ContainsKey("redis");
        await Assert.That(config.Resources).ContainsKey("api");
    }
}
