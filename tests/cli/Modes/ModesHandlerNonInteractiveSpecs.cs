using NSubstitute;

using Spectre.Console.Testing;

using Spire.Cli.Services.Configuration;

namespace Spire.Cli.Tests.Modes;

/// <summary>
/// Tests for <see cref="ModesHandler"/> non-interactive mode.
/// </summary>
public class ModesHandlerNonInteractiveSpecs
{
    [Test]
    public async Task Execute_NonInteractive_SetsMode()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        GlobalSharedResources? savedResources = null;
        writer.SaveGlobalAsync(Arg.Do<GlobalSharedResources>(r => savedResources = r), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteNonInteractiveAsync("postgres", Mode.Project);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Set 'postgres' to Project mode");
        await Assert.That(savedResources).IsNotNull();
        await Assert.That(savedResources!.Resources["postgres"].Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task Execute_NonInteractive_InvalidId_ReturnsError()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteNonInteractiveAsync("nonexistent", Mode.Project);

        // Assert
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Resource 'nonexistent' not found");
        await writer.DidNotReceive().SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_NonInteractive_SameModeNoOp()
    {
        // Arrange
        var console = new TestConsole();
        var writer = Substitute.For<ISharedResourcesWriter>();
        var resources = CreateTestResources();
        var handler = new ModesHandler(console, writer, () => resources);

        // Act
        var result = await handler.ExecuteNonInteractiveAsync("postgres", Mode.Container);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("already in Container mode");
        await writer.DidNotReceive().SaveGlobalAsync(Arg.Any<GlobalSharedResources>(), Arg.Any<CancellationToken>());
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