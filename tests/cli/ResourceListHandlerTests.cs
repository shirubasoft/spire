using System.Text.Json;
using Spire.Cli;

namespace Spire.Cli.Tests;

/// <summary>
/// Tests for listing resources when no resources exist.
/// </summary>
public class WhenNoResourcesExistSpecs
{
    [Test]
    public async Task Execute_WhenNoResources_ReturnsZeroExitCode()
    {
        var handler = new ResourceListHandler();
        var emptyResources = GlobalSharedResources.Empty;
        using var output = new StringWriter();

        var exitCode = handler.Execute(emptyResources, output);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_WhenNoResources_OutputsEmptyResourcesJson()
    {
        var handler = new ResourceListHandler();
        var emptyResources = GlobalSharedResources.Empty;
        using var output = new StringWriter();

        handler.Execute(emptyResources, output);

        var result = output.ToString();
        await Assert.That(result).Contains("\"resources\"");
        await Assert.That(result).Contains("{}");
    }

    [Test]
    public async Task Execute_WhenNoResources_OutputIsValidJson()
    {
        var handler = new ResourceListHandler();
        var emptyResources = GlobalSharedResources.Empty;
        using var output = new StringWriter();

        handler.Execute(emptyResources, output);

        var result = output.ToString();
        await Assert.That(() => JsonDocument.Parse(result)).ThrowsNothing();
    }
}

/// <summary>
/// Tests for listing resources when resources exist.
/// </summary>
public class WhenResourcesExistSpecs
{
    private static GlobalSharedResources CreateResourcesWithPostgres()
    {
        return new GlobalSharedResources
        {
            Resources = new Dictionary<string, SharedResource>
            {
                ["postgres"] = new SharedResource
                {
                    Mode = Mode.Container,
                    ContainerMode = new ContainerModeSettings
                    {
                        ImageName = "postgres",
                        ImageRegistry = "docker.io",
                        ImageTag = "latest",
                        BuildCommand = "docker build -t postgres .",
                        BuildWorkingDirectory = "/home/user/projects/postgres"
                    },
                    ProjectMode = null,
                    GitRepository = new GitRepositorySettings
                    {
                        Url = new Uri("https://github.com/org/repo"),
                        DefaultBranch = "main"
                    }
                }
            }
        };
    }

    [Test]
    public async Task Execute_WhenResourcesExist_ReturnsZeroExitCode()
    {
        var handler = new ResourceListHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();

        var exitCode = handler.Execute(resources, output);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_WhenResourcesExist_OutputsResourcesJson()
    {
        var handler = new ResourceListHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();

        handler.Execute(resources, output);

        var result = output.ToString();
        await Assert.That(result).Contains("\"resources\"");
        await Assert.That(result).Contains("\"postgres\"");
        await Assert.That(result).Contains("\"mode\"");
        await Assert.That(result).Contains("\"container\"");
    }

    [Test]
    public async Task Execute_WhenResourcesExist_OutputIsValidJson()
    {
        var handler = new ResourceListHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();

        handler.Execute(resources, output);

        var result = output.ToString();
        await Assert.That(() => JsonDocument.Parse(result)).ThrowsNothing();
    }

    [Test]
    public async Task Execute_WhenResourcesExist_IncludesContainerModeSettings()
    {
        var handler = new ResourceListHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();

        handler.Execute(resources, output);

        var result = output.ToString();
        await Assert.That(result).Contains("\"containerMode\"");
        await Assert.That(result).Contains("\"imageName\"");
        await Assert.That(result).Contains("\"imageRegistry\"");
        await Assert.That(result).Contains("\"imageTag\"");
    }

    [Test]
    public async Task Execute_WhenResourcesExist_IncludesGitRepositorySettings()
    {
        var handler = new ResourceListHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();

        handler.Execute(resources, output);

        var result = output.ToString();
        await Assert.That(result).Contains("\"gitRepository\"");
        await Assert.That(result).Contains("\"url\"");
        await Assert.That(result).Contains("\"defaultBranch\"");
    }
}
