using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.Services.Configuration;

/// <summary>
/// Integration tests for RepositorySharedResourcesReader.
/// Tests the actual .aspire/settings.json format per the aspireSettings schema.
/// </summary>
public class RepositorySharedResourcesReaderSpecs
{
    private string _tempDirectory = null!;
    private string _aspireDirectory = null!;
    private string _settingsFilePath = null!;
    private RepositorySharedResourcesReader _reader = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"spire-test-{Guid.NewGuid()}");
        _aspireDirectory = Path.Combine(_tempDirectory, ".aspire");
        _settingsFilePath = Path.Combine(_aspireDirectory, "settings.json");
        Directory.CreateDirectory(_aspireDirectory);
        _reader = new RepositorySharedResourcesReader();
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
    public async Task ReadAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange - no file created

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReadAsync_WhenFileIsEmpty_ReturnsEmptyResources()
    {
        // Arrange
        await File.WriteAllTextAsync(_settingsFilePath, "");

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).IsEmpty();
    }

    [Test]
    public async Task ReadAsync_WhenFileHasOnlyAppHostPath_ReturnsEmptyResources()
    {
        // Arrange - per schema, appHostPath is required but sharedResources is optional
        const string json = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj"
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).IsEmpty();
    }

    [Test]
    public async Task ReadAsync_WhenFileHasAspireSettingsWrapper_ExtractsSharedResources()
    {
        // Arrange - this is the actual .aspire/settings.json format per aspireSettings schema
        const string json = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {
                        "my-service": {
                            "mode": "container",
                            "containerMode": {
                                "imageName": "my-service",
                                "imageRegistry": "docker.io",
                                "imageTag": "latest",
                                "buildCommand": "docker build -t my-service .",
                                "buildWorkingDirectory": "./src/MyService"
                            }
                        }
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).ContainsKey("my-service");
        await Assert.That(result.Resources["my-service"].Mode).IsEqualTo(Mode.Container);
        await Assert.That(result.Resources["my-service"].ContainerMode).IsNotNull();
        await Assert.That(result.Resources["my-service"].ContainerMode!.ImageName).IsEqualTo("my-service");
    }

    [Test]
    public async Task ReadAsync_WhenFileHasMinimalResource_DeserializesWithNullOptionalFields()
    {
        // Arrange - resource with only required mode, no containerMode/projectMode/gitRepository
        const string json = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {
                        "postgres": {
                            "mode": "container"
                        }
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).ContainsKey("postgres");

        var postgres = result.Resources["postgres"];
        await Assert.That(postgres.Mode).IsEqualTo(Mode.Container);
        await Assert.That(postgres.ContainerMode).IsNull();
        await Assert.That(postgres.ProjectMode).IsNull();
        await Assert.That(postgres.GitRepository).IsNull();
    }

    [Test]
    public async Task ReadAsync_WhenFileHasMultipleResources_DeserializesAll()
    {
        // Arrange
        const string json = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
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
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources.Count).IsEqualTo(3);
        await Assert.That(result.Resources).ContainsKey("postgres");
        await Assert.That(result.Resources).ContainsKey("redis");
        await Assert.That(result.Resources).ContainsKey("api");
    }

    [Test]
    public async Task ReadAsync_WhenFileHasExternalResources_DeserializesExternalResources()
    {
        // Arrange
        const string json = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {},
                    "externalResources": [
                        {
                            "url": "https://github.com/org/shared-resources",
                            "branch": "main"
                        }
                    ]
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ExternalResources.Count).IsEqualTo(1);
        await Assert.That(result.ExternalResources[0].Url.ToString()).IsEqualTo("https://github.com/org/shared-resources");
    }

    [Test]
    public async Task ReadAsync_SupportsJsonComments()
    {
        // Arrange - JSON with comments (common in settings files)
        const string json = """
            {
                // Path to the AppHost project
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {
                        // Database resource
                        "postgres": {
                            "mode": "container"
                        }
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).ContainsKey("postgres");
    }

    [Test]
    public async Task ReadAsync_SupportsTrailingCommas()
    {
        // Arrange - JSON with trailing commas (common when hand-editing)
        const string json = """
            {
                "appHostPath": "src/MyApp.AppHost/MyApp.AppHost.csproj",
                "sharedResources": {
                    "resources": {
                        "postgres": {
                            "mode": "container",
                        },
                    },
                },
            }
            """;
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _reader.ReadAsync(_tempDirectory);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).ContainsKey("postgres");
    }

    [Test]
    public async Task SettingsFileExists_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        await File.WriteAllTextAsync(_settingsFilePath, "{}");

        // Act
        var result = _reader.SettingsFileExists(_tempDirectory);

        // Assert
        await Assert.That(result).IsEqualTo(true);
    }

    [Test]
    public async Task SettingsFileExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = _reader.SettingsFileExists(_tempDirectory);

        // Assert
        await Assert.That(result).IsEqualTo(false);
    }

    [Test]
    public async Task GetSettingsFilePath_ReturnsCorrectPath()
    {
        // Act
        var result = _reader.GetSettingsFilePath(_tempDirectory);

        // Assert
        await Assert.That(result).IsEqualTo(_settingsFilePath);
    }
}
