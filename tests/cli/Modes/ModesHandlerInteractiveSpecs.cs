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
        console.Input.PushKey(ConsoleKey.Enter); // Confirm immediately

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("[Container]");
        await Assert.That(console.Output).Contains("postgres");
        await Assert.That(console.Output).Contains("[Project]");
        await Assert.That(console.Output).Contains("my-service");
    }

    [Test]
    public async Task Execute_Interactive_TogglesSelectedResource()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Spacebar); // Toggle postgres (Container -> Project)
        console.Input.PushKey(ConsoleKey.Enter);    // Confirm

        var writer = Substitute.For<ISharedResourcesWriter>();
        GlobalSharedResources? savedResources = null;
        writer.SaveGlobalAsync(Arg.Do<GlobalSharedResources>(r => savedResources = r), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

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
        console.Input.PushKey(ConsoleKey.Spacebar); // Toggle first resource
        console.Input.PushKey(ConsoleKey.Enter);    // Confirm

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

        // Act
        await handler.ExecuteInteractiveAsync();

        // Assert
        await writer.Received(1).SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_Interactive_EscapeDoesNotSave()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Spacebar); // Toggle a resource
        console.Input.PushKey(ConsoleKey.Escape);   // Cancel without saving

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.DidNotReceive().SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
        await Assert.That(console.Output).Contains("No changes saved");
    }

    [Test]
    public async Task Execute_Interactive_ConfirmWithNoChanges_DoesNotSave()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Enter); // Confirm immediately without toggling

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await writer.DidNotReceive().SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
        await Assert.That(console.Output).Contains("No changes made");
    }

    [Test]
    public async Task Execute_Interactive_TogglesMultipleResources()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Spacebar);  // Toggle postgres (Container -> Project)
        console.Input.PushKey(ConsoleKey.DownArrow);  // Navigate to my-service
        console.Input.PushKey(ConsoleKey.Spacebar);  // Toggle my-service (Project -> Container)
        console.Input.PushKey(ConsoleKey.Enter);     // Confirm

        var writer = Substitute.For<ISharedResourcesWriter>();
        GlobalSharedResources? savedResources = null;
        writer.SaveGlobalAsync(Arg.Do<GlobalSharedResources>(r => savedResources = r), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(savedResources).IsNotNull();
        await Assert.That(savedResources!.Resources["postgres"].Mode).IsEqualTo(Mode.Project);
        await Assert.That(savedResources!.Resources["my-service"].Mode).IsEqualTo(Mode.Container);
    }

    [Test]
    public async Task Execute_Interactive_DoubleToggleRevertsChange()
    {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Spacebar); // Toggle postgres (Container -> Project)
        console.Input.PushKey(ConsoleKey.Spacebar); // Toggle postgres again (Project -> Container)
        console.Input.PushKey(ConsoleKey.Enter);    // Confirm

        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

        // Act
        var result = await handler.ExecuteInteractiveAsync();

        // Assert — double toggle = no net change
        await Assert.That(result).IsEqualTo(0);
        await writer.DidNotReceive().SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
        await Assert.That(console.Output).Contains("No changes made");
    }

    [Test]
    public async Task Execute_Interactive_WhenNoResources_ShowsEmptyMessage()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = GlobalSharedResources.Empty;
        var globalReader = Substitute.For<IGlobalSharedResourcesReader>();
        globalReader.GetSharedResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(resources);
        var handler = new ModesHandler(console, writer, globalReader);

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
            ProjectPath = "/path/to/project.csproj"
        },
        GitRepository = null
    };
}
