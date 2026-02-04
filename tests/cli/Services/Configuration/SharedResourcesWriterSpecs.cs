using System.Text.Json;
using System.Text.Json.Serialization;

using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.Services.Configuration;

/// <summary>
/// Tests for SharedResourcesWriter preserving existing settings.
/// </summary>
public class SharedResourcesWriterPreservesExistingSettingsSpecs
{
    private string _tempDirectory = null!;
    private string _aspireDirectory = null!;
    private string _settingsFilePath = null!;
    private SharedResourcesWriter _writer = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"spire-test-{Guid.NewGuid()}");
        _aspireDirectory = Path.Combine(_tempDirectory, ".aspire");
        _settingsFilePath = Path.Combine(_aspireDirectory, "settings.json");
        Directory.CreateDirectory(_aspireDirectory);
        _writer = new SharedResourcesWriter();
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Test]
    public async Task SaveRepositoryAsync_WhenFileHasAppHostPath_PreservesIt()
    {
        // Arrange - existing file with appHostPath
        const string existingJson = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {}
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, existingJson);

        var newResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "my-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "docker build .",
                        BuildWorkingDirectory = "./src/MyService"
                    }
                }
            }
        };

        // Act
        await _writer.SaveRepositoryAsync(newResources, _tempDirectory);

        // Assert
        var savedJson = await File.ReadAllTextAsync(_settingsFilePath);
        using var document = JsonDocument.Parse(savedJson);

        await Assert.That(document.RootElement.TryGetProperty("appHostPath", out var appHostPath)).IsTrue();
        await Assert.That(appHostPath.GetString()).IsEqualTo("src/MyApp.AppHost/MyApp.AppHost.csproj");
    }

    [Test]
    public async Task SaveRepositoryAsync_WhenAddingSecondResource_PreservesAppHostPath()
    {
        // Arrange - existing file with appHostPath and one resource
        const string existingJson = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {
                        "first-service": {
                            "mode": "container"
                        }
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, existingJson);

        var newResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["first-service"] = new SharedResource { Mode = Mode.Container },
                ["second-service"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "second-service",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "docker build .",
                        BuildWorkingDirectory = "./src/SecondService"
                    }
                }
            }
        };

        // Act
        await _writer.SaveRepositoryAsync(newResources, _tempDirectory);

        // Assert
        var savedJson = await File.ReadAllTextAsync(_settingsFilePath);
        using var document = JsonDocument.Parse(savedJson);

        await Assert.That(document.RootElement.TryGetProperty("appHostPath", out var appHostPath)).IsTrue();
        await Assert.That(appHostPath.GetString()).IsEqualTo("src/MyApp.AppHost/MyApp.AppHost.csproj");

        await Assert.That(document.RootElement.TryGetProperty("sharedResources", out var sharedResources)).IsTrue();
        await Assert.That(sharedResources.TryGetProperty("resources", out var resources)).IsTrue();
        await Assert.That(resources.TryGetProperty("first-service", out _)).IsTrue();
        await Assert.That(resources.TryGetProperty("second-service", out _)).IsTrue();
    }

    [Test]
    public async Task SaveRepositoryAsync_WhenFileDoesNotExist_CreatesWithSharedResourcesWrapper()
    {
        // Arrange - no existing file
        var newResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource { Mode = Mode.Container }
            }
        };

        // Act
        await _writer.SaveRepositoryAsync(newResources, _tempDirectory);

        // Assert
        var savedJson = await File.ReadAllTextAsync(_settingsFilePath);
        using var document = JsonDocument.Parse(savedJson);

        await Assert.That(document.RootElement.TryGetProperty("sharedResources", out var sharedResources)).IsTrue();
        await Assert.That(sharedResources.TryGetProperty("resources", out var resources)).IsTrue();
        await Assert.That(resources.TryGetProperty("my-service", out _)).IsTrue();
    }

    [Test]
    public async Task SaveRepositoryAsync_WhenExistingFileHasNoAppHostPath_DoesNotAddOne()
    {
        // Arrange - existing file without appHostPath (edge case)
        const string existingJson = """
            {
                "sharedResources": {
                    "resources": {}
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, existingJson);

        var newResources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource { Mode = Mode.Container }
            }
        };

        // Act
        await _writer.SaveRepositoryAsync(newResources, _tempDirectory);

        // Assert
        var savedJson = await File.ReadAllTextAsync(_settingsFilePath);
        using var document = JsonDocument.Parse(savedJson);

        // appHostPath should be null/absent since it wasn't in the original
        var hasAppHostPath = document.RootElement.TryGetProperty("appHostPath", out var appHostPath);
        await Assert.That(!hasAppHostPath || appHostPath.ValueKind == JsonValueKind.Null).IsTrue();
    }
}

/// <summary>
/// Tests that the writer produces correct JSON structure without extraneous fields.
/// </summary>
public class SharedResourcesWriterJsonStructureSpecs
{
    private string _tempDirectory = null!;
    private SharedResourcesWriter _writer = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"spire-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(_tempDirectory, ".aspire"));
        _writer = new SharedResourcesWriter();
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Test]
    public async Task SaveRepositoryAsync_DoesNotWriteCountField()
    {
        // Arrange
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["service-a"] = new SharedResource { Mode = Mode.Container },
                ["service-b"] = new SharedResource { Mode = Mode.Project }
            }
        };

        // Act
        await _writer.SaveRepositoryAsync(resources, _tempDirectory);

        // Assert
        var json = await File.ReadAllTextAsync(
            Path.Combine(_tempDirectory, ".aspire", "settings.json"));

        await Assert.That(json).DoesNotContain("\"count\"");
    }

    [Test]
    public async Task SaveRepositoryAsync_IncludesSchemaField()
    {
        // Arrange
        var resources = new RepositorySharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource { Mode = Mode.Container }
            }
        };

        // Act
        await _writer.SaveRepositoryAsync(resources, _tempDirectory);

        // Assert
        var json = await File.ReadAllTextAsync(
            Path.Combine(_tempDirectory, ".aspire", "settings.json"));
        using var document = JsonDocument.Parse(json);

        await Assert.That(document.RootElement.TryGetProperty("$schema", out var schema)).IsTrue();
        await Assert.That(schema.GetString())
            .IsEqualTo("https://raw.githubusercontent.com/shirubasoft/spire/main/schemas/aspire-settings.schema.json");
    }
}

/// <summary>
/// Tests that GlobalSharedResources serialization produces correct JSON.
/// </summary>
public class GlobalSharedResourcesSerializationSpecs
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Test]
    public async Task Serialize_DoesNotWriteCountField()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["service-a"] = new SharedResource { Mode = Mode.Container },
                ["service-b"] = new SharedResource { Mode = Mode.Project }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(resources, JsonOptions);

        // Assert
        await Assert.That(json).DoesNotContain("\"count\"");
    }

    [Test]
    public async Task Serialize_WithSchema_IncludesSchemaField()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Schema = "https://raw.githubusercontent.com/shirubasoft/spire/main/schemas/shared-resources-global.schema.json",
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource { Mode = Mode.Container }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(resources, JsonOptions);
        using var document = JsonDocument.Parse(json);

        // Assert
        await Assert.That(document.RootElement.TryGetProperty("$schema", out var schema)).IsTrue();
        await Assert.That(schema.GetString())
            .IsEqualTo("https://raw.githubusercontent.com/shirubasoft/spire/main/schemas/shared-resources-global.schema.json");
    }

    [Test]
    public async Task Serialize_WithoutSchema_OmitsSchemaField()
    {
        // Arrange
        var resources = new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["my-service"] = new SharedResource { Mode = Mode.Container }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(resources, JsonOptions);

        // Assert
        await Assert.That(json).DoesNotContain("\"$schema\"");
    }
}
