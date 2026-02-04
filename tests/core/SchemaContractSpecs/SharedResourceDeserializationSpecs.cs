using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spire.Tests.SchemaContractSpecs;

/// <summary>
/// Tests that SharedResource deserializes correctly per the JSON schema.
/// The schema defines mode as required, but containerMode, projectMode, and gitRepository as optional.
/// </summary>
public class SharedResourceDeserializationSpecs
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Test]
    public async Task Deserialize_WithOnlyMode_Succeeds()
    {
        // Arrange - minimal resource per schema (only mode is required)
        const string json = """
            {
                "mode": "container"
            }
            """;

        // Act
        var resource = JsonSerializer.Deserialize<SharedResource>(json, JsonOptions);

        // Assert
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.Mode).IsEqualTo(Mode.Container);
        await Assert.That(resource.ContainerMode).IsNull();
        await Assert.That(resource.ProjectMode).IsNull();
        await Assert.That(resource.GitRepository).IsNull();
    }

    [Test]
    public async Task Deserialize_WithProjectMode_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "mode": "project"
            }
            """;

        // Act
        var resource = JsonSerializer.Deserialize<SharedResource>(json, JsonOptions);

        // Assert
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task Deserialize_WithContainerModeSettings_Succeeds()
    {
        // Arrange - resource with containerMode settings
        const string json = """
            {
                "mode": "container",
                "containerMode": {
                    "imageName": "my-service",
                    "imageRegistry": "docker.io",
                    "imageTag": "latest",
                    "buildCommand": "docker build -t my-service .",
                    "buildWorkingDirectory": "./src/MyService"
                }
            }
            """;

        // Act
        var resource = JsonSerializer.Deserialize<SharedResource>(json, JsonOptions);

        // Assert
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.ContainerMode).IsNotNull();
        await Assert.That(resource.ContainerMode!.ImageName).IsEqualTo("my-service");
        await Assert.That(resource.ContainerMode.ImageRegistry).IsEqualTo("docker.io");
        await Assert.That(resource.ContainerMode.ImageTag).IsEqualTo("latest");
        await Assert.That(resource.ContainerMode.BuildCommand).IsEqualTo("docker build -t my-service .");
        await Assert.That(resource.ContainerMode.BuildWorkingDirectory).IsEqualTo("./src/MyService");
    }

    [Test]
    public async Task Deserialize_WithProjectModeSettings_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "mode": "project",
                "projectMode": {
                    "projectPath": "./src/MyService/MyService.csproj"
                }
            }
            """;

        // Act
        var resource = JsonSerializer.Deserialize<SharedResource>(json, JsonOptions);

        // Assert
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.ProjectMode).IsNotNull();
        await Assert.That(resource.ProjectMode!.ProjectPath).IsEqualTo("./src/MyService/MyService.csproj");
    }

    [Test]
    public async Task Deserialize_WithGitRepositorySettings_Succeeds()
    {
        // Arrange
        const string json = """
            {
                "mode": "container",
                "gitRepository": {
                    "url": "https://github.com/org/repo",
                    "defaultBranch": "main"
                }
            }
            """;

        // Act
        var resource = JsonSerializer.Deserialize<SharedResource>(json, JsonOptions);

        // Assert
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.GitRepository).IsNotNull();
        await Assert.That(resource.GitRepository!.Url.ToString()).IsEqualTo("https://github.com/org/repo");
        await Assert.That(resource.GitRepository.DefaultBranch).IsEqualTo("main");
    }

    [Test]
    public async Task Deserialize_WithAllSettings_Succeeds()
    {
        // Arrange - full resource with all optional fields
        const string json = """
            {
                "mode": "container",
                "containerMode": {
                    "imageName": "my-service",
                    "imageRegistry": "docker.io",
                    "imageTag": "latest",
                    "buildCommand": "docker build -t my-service .",
                    "buildWorkingDirectory": "./src/MyService"
                },
                "projectMode": {
                    "projectPath": "./src/MyService/MyService.csproj"
                },
                "gitRepository": {
                    "url": "https://github.com/org/repo",
                    "defaultBranch": "develop"
                }
            }
            """;

        // Act
        var resource = JsonSerializer.Deserialize<SharedResource>(json, JsonOptions);

        // Assert
        await Assert.That(resource).IsNotNull();
        await Assert.That(resource!.Mode).IsEqualTo(Mode.Container);
        await Assert.That(resource.ContainerMode).IsNotNull();
        await Assert.That(resource.ProjectMode).IsNotNull();
        await Assert.That(resource.GitRepository).IsNotNull();
    }
}
