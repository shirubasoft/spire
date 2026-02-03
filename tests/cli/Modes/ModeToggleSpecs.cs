namespace Spire.Cli.Tests.Modes;

/// <summary>
/// Tests for mode toggle logic.
/// </summary>
public class ModeToggleSpecs
{
    [Test]
    public async Task Toggle_FromContainer_ReturnsProject()
    {
        var result = ModesHandler.ToggleMode(Mode.Container);

        await Assert.That(result).IsEqualTo(Mode.Project);
    }

    [Test]
    public async Task Toggle_FromProject_ReturnsContainer()
    {
        var result = ModesHandler.ToggleMode(Mode.Project);

        await Assert.That(result).IsEqualTo(Mode.Container);
    }

    [Test]
    public async Task Toggle_PreservesAllSettings()
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

        var newMode = ModesHandler.ToggleMode(resource.Mode);
        var result = resource.WithMode(newMode);

        await Assert.That(result.Mode).IsEqualTo(Mode.Project);
        await Assert.That(result.ContainerMode).IsEqualTo(containerSettings);
        await Assert.That(result.ProjectMode).IsEqualTo(projectSettings);
        await Assert.That(result.GitRepository).IsEqualTo(gitSettings);
    }

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
