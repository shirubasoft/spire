namespace Spire.Cli.Tests.Modes;

/// <summary>
/// Tests for <see cref="GlobalSharedResources.UpdateResource"/> method.
/// </summary>
public class GlobalSharedResourcesUpdateSpecs
{
    [Test]
    public async Task UpdateResource_ReturnsNewInstance()
    {
        var resources = CreateTestResources();
        var updatedResource = resources.Resources["postgres"].WithMode(Mode.Project);

        var result = resources.UpdateResource("postgres", updatedResource);

        await Assert.That(result).IsNotSameReferenceAs(resources);
    }

    [Test]
    public async Task UpdateResource_UpdatesTargetResource()
    {
        var resources = CreateTestResources();
        var updatedResource = resources.Resources["postgres"].WithMode(Mode.Project);

        var result = resources.UpdateResource("postgres", updatedResource);

        await Assert.That(result.Resources["postgres"].Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task UpdateResource_PreservesOtherResources()
    {
        var resources = CreateTestResources();
        var originalMyService = resources.Resources["my-service"];
        var updatedResource = resources.Resources["postgres"].WithMode(Mode.Project);

        var result = resources.UpdateResource("postgres", updatedResource);

        await Assert.That(result.Resources["my-service"]).IsEqualTo(originalMyService);
    }

    [Test]
    public async Task UpdateResource_AddsNewResource()
    {
        var resources = CreateTestResources();
        var newResource = CreateTestResource(Mode.Container);

        var result = resources.UpdateResource("new-resource", newResource);

        await Assert.That(result.Resources.ContainsKey("new-resource")).IsTrue();
        await Assert.That(result.Resources["new-resource"]).IsEqualTo(newResource);
        await Assert.That(result.Resources.Count).IsEqualTo(3);
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
