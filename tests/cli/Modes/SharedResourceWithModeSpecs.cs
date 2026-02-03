namespace Spire.Cli.Tests.Modes;

/// <summary>
/// Tests for <see cref="SharedResource.WithMode"/> method.
/// </summary>
public class SharedResourceWithModeSpecs
{
    [Test]
    public async Task WithMode_ReturnsNewInstance()
    {
        var resource = CreateTestResource(Mode.Container);

        var result = resource.WithMode(Mode.Project);

        await Assert.That(result).IsNotSameReferenceAs(resource);
        await Assert.That(result.Mode).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task WithMode_SameMode_ReturnsSameValues()
    {
        var resource = CreateTestResource(Mode.Container);

        var result = resource.WithMode(Mode.Container);

        await Assert.That(result.Mode).IsEqualTo(Mode.Container);
        await Assert.That(result.ContainerMode).IsEqualTo(resource.ContainerMode);
        await Assert.That(result.ProjectMode).IsEqualTo(resource.ProjectMode);
        await Assert.That(result.GitRepository).IsEqualTo(resource.GitRepository);
    }

    [Test]
    public async Task WithMode_PreservesAllSettings()
    {
        var containerSettings = CreateContainerSettings();
        var projectSettings = CreateProjectSettings();
        var gitSettings = CreateGitSettings();

        var resource = new SharedResource
        {
            Mode = Mode.Container,
            ContainerMode = containerSettings,
            ProjectMode = projectSettings,
            GitRepository = gitSettings
        };

        var result = resource.WithMode(Mode.Project);

        await Assert.That(result.Mode).IsEqualTo(Mode.Project);
        await Assert.That(result.ContainerMode).IsEqualTo(containerSettings);
        await Assert.That(result.ProjectMode).IsEqualTo(projectSettings);
        await Assert.That(result.GitRepository).IsEqualTo(gitSettings);
    }

    private static SharedResource CreateTestResource(Mode mode) => new()
    {
        Mode = mode,
        ContainerMode = CreateContainerSettings(),
        ProjectMode = CreateProjectSettings(),
        GitRepository = null
    };

    private static ContainerModeSettings CreateContainerSettings() => new()
    {
        ImageName = "test",
        ImageRegistry = "docker.io",
        ImageTag = "latest",
        BuildCommand = "docker build .",
        BuildWorkingDirectory = "/path"
    };

    private static ProjectModeSettings CreateProjectSettings() => new()
    {
        ProjectDirectory = "/path/to/project"
    };

    private static GitRepositorySettings CreateGitSettings() => new()
    {
        Url = new Uri("https://github.com/test/repo"),
        DefaultBranch = "main"
    };
}
