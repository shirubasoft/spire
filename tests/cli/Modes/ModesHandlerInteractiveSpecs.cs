using NSubstitute;

using Spectre.Console.Testing;

using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.Modes;

/// <summary>
/// Tests for <see cref="ModesHandler"/> interactive mode.
/// </summary>
public class ModesHandlerInteractiveSpecs
{
    [Test]
    public async Task Execute_Interactive_ShowsResourceList()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        // Navigate down to Exit and select
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("[Container] postgres");
        await Assert.That(console.Output).Contains("[Project] my-service");
    }

    [Test]
    public async Task Execute_Interactive_TogglesSelectedResource()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        // Select first resource (postgres), then Exit
        console.Input.PushKey(ConsoleKey.Enter);       // Select postgres (Container -> Project)
        console.Input.PushKey(ConsoleKey.DownArrow);   // Navigate to second
        console.Input.PushKey(ConsoleKey.DownArrow);   // Navigate to Exit
        console.Input.PushKey(ConsoleKey.Enter);       // Select Exit

        var writer = Substitute.For<ISharedResourcesWriter>();
        GlobalSharedResources? savedResources = null;
        writer.SaveGlobalAsync(Arg.Do<GlobalSharedResources>(r => savedResources = r), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Toggled 'postgres' from Container to Project");
        await Assert.That(savedResources).IsNotNull();
        await Assert.That(savedResources!.Resources["postgres"].Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task Execute_Interactive_SavesChanges()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        // Toggle first resource, then exit
        console.Input.PushKey(ConsoleKey.Enter);       // Select first
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);       // Exit

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        await handler.ExecuteInteractiveAsync();

        // Assert
        await writer.Received(1).SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_Interactive_ExitDoesNotSave()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        // Go directly to Exit (last option)
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.DidNotReceive().SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_Interactive_WhenNoResources_ShowsEmptyMessage()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = GlobalSharedResources.Empty;
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No resources configured");
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
}
