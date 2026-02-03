namespace Spire.Cli.Tests.TestHelpers;

/// <summary>
/// Helper methods for creating test resources.
/// </summary>
public static class ResourceTestHelpers
{
    /// <summary>
    /// Creates a sample SharedResource for testing.
    /// </summary>
    public static SharedResource CreateResource(string name = "test")
    {
        return new SharedResource
        {
            Mode = Mode.Container,
            ContainerMode = new ContainerModeSettings
            {
                ImageName = name,
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
    /// Creates a GlobalSharedResources with the given resource IDs.
    /// </summary>
    public static GlobalSharedResources CreateGlobalResources(params string[] ids)
    {
        var resources = new Dictionary<string, SharedResource>();
        foreach (var id in ids)
        {
            resources[id] = CreateResource(id);
        }

        return new GlobalSharedResources { Resources = resources };
    }

    /// <summary>
    /// Creates a RepositorySharedResources with the given resource IDs.
    /// </summary>
    public static RepositorySharedResources CreateRepoResources(params string[] ids)
    {
        var resources = new Dictionary<string, SharedResource>();
        foreach (var id in ids)
        {
            resources[id] = CreateResource(id);
        }

        return new RepositorySharedResources { Resources = resources };
    }
}
