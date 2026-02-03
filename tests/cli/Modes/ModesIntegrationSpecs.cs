using System.Text.Json;
using System.Text.Json.Serialization;

using Spectre.Console.Testing;

using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.Modes;

/// <summary>
/// Integration tests for the modes command.
/// </summary>
public class ModesIntegrationSpecs : IAsyncDisposable
{
    private string? _tempConfigDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Before(Test)]
    public void SetUp()
    {
        _tempConfigDir = Path.Combine(Path.GetTempPath(), $"spire-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempConfigDir);
    }

    [After(Test)]
    public async Task TearDown()
    {
        await DisposeAsync();
    }

    public ValueTask DisposeAsync()
    {
        if (_tempConfigDir is not null && Directory.Exists(_tempConfigDir))
        {
            try
            {
                Directory.Delete(_tempConfigDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        return ValueTask.CompletedTask;
    }

    [Test]
    public async Task Modes_Interactive_LoadsAllResources()
    {
        // Arrange
        var configPath = SetupTestConfig(CreateTestResources());

        var console = new TestConsole();
        console.Interactive();
        // Exit immediately
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var writer = new TestSharedResourcesWriter(configPath);
        var handler = new ModesHandler(console, writer, () => LoadResources(configPath));

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("[Container] postgres");
        await Assert.That(console.Output).Contains("[Project] my-service");
    }

    [Test]
    public async Task Modes_ToggleMode_PersistsChange()
    {
        // Arrange
        var configPath = SetupTestConfig(CreateTestResources());

        var console = new TestConsole();
        console.Interactive();
        // Toggle postgres then exit
        console.Input.PushKey(ConsoleKey.Enter);       // Select postgres
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);       // Exit

        var writer = new TestSharedResourcesWriter(configPath);
        var handler = new ModesHandler(console, writer, () => LoadResources(configPath));

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        var savedResources = LoadResources(configPath);
        await Assert.That(savedResources.Resources["postgres"].Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task Modes_NonInteractive_SetsMode()
    {
        // Arrange
        var configPath = SetupTestConfig(CreateTestResources());

        var console = new TestConsole();
        var writer = new TestSharedResourcesWriter(configPath);
        var handler = new ModesHandler(console, writer, () => LoadResources(configPath));

        // Act
        var result = await handler.ExecuteNonInteractiveAsync("postgres", Mode.Project);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        var savedResources = LoadResources(configPath);
        await Assert.That(savedResources.Resources["postgres"].Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task Modes_WhenNoResources_ShowsEmptyMessage()
    {
        // Arrange
        var configPath = SetupTestConfig(GlobalSharedResources.Empty);

        var console = new TestConsole();
        var writer = new TestSharedResourcesWriter(configPath);
        var handler = new ModesHandler(console, writer, () => LoadResources(configPath));

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No resources configured");
    }

    private string SetupTestConfig(GlobalSharedResources resources)
    {
        var configPath = Path.Combine(_tempConfigDir!, "aspire-shared-resources.json");
        var json = JsonSerializer.Serialize(resources, JsonOptions);
        File.WriteAllText(configPath, json);
        return configPath;
    }

    private static GlobalSharedResources LoadResources(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return GlobalSharedResources.Empty;
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<GlobalSharedResources>(json, JsonOptions)
            ?? GlobalSharedResources.Empty;
    }

    private static GlobalSharedResources CreateTestResources() => new()
    {
        Resources = new Dictionary<string, SharedResource>
        {
            ["postgres"] = CreateTestResource(Mode.Container),
            ["my-service"] = CreateTestResource(Mode.Project)
        }
    };

    private static SharedResource CreateTestResource(Mode mode) => new()
    {
        Mode = mode,
        ContainerMode = new ContainerModeSettings
        {
            ImageName = "test",
            ImageRegistry = "docker.io",
            ImageTag = "latest",
            BuildCommand = "docker build .",
            BuildWorkingDirectory = "/path"
        },
        ProjectMode = new ProjectModeSettings
        {
            ProjectDirectory = "/path/to/project"
        },
        GitRepository = null
    };

    /// <summary>
    /// A test writer that writes to a specific path.
    /// </summary>
    private sealed class TestSharedResourcesWriter : ISharedResourcesWriter
    {
        private readonly string _configPath;

        public TestSharedResourcesWriter(string configPath)
        {
            _configPath = configPath;
        }

        public async Task SaveGlobalAsync(GlobalSharedResources resources, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(resources, JsonOptions);
            await File.WriteAllTextAsync(_configPath, json, cancellationToken);
        }

        public Task SaveRepositoryAsync(RepositorySharedResources resources, string repoPath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
