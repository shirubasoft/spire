using System.Text.Json;

using Spire.Cli;

namespace Spire.Cli.Tests;

/// <summary>
/// Tests for getting info when the resource exists.
/// </summary>
public class WhenResourceInfoExistsSpecs
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
    public async Task Execute_WhenResourceExists_ReturnsZeroExitCode()
    {
        var handler = new ResourceInfoHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = handler.Execute("postgres", resources, output, error);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_WhenResourceExists_OutputsResourceJson()
    {
        var handler = new ResourceInfoHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute("postgres", resources, output, error);

        var result = output.ToString();
        await Assert.That(result).Contains("\"mode\"");
        await Assert.That(result).Contains("\"container\"");
        await Assert.That(result).Contains("\"containerMode\"");
    }

    [Test]
    public async Task Execute_WhenResourceExists_OutputIsValidJson()
    {
        var handler = new ResourceInfoHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute("postgres", resources, output, error);

        var result = output.ToString();
        await Assert.That(() => JsonDocument.Parse(result)).ThrowsNothing();
    }

    [Test]
    public async Task Execute_WhenResourceExists_DoesNotWriteToStderr()
    {
        var handler = new ResourceInfoHandler();
        var resources = CreateResourcesWithPostgres();
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute("postgres", resources, output, error);

        await Assert.That(error.ToString()).IsEmpty();
    }
}

/// <summary>
/// Tests for getting info when the resource does not exist.
/// </summary>
public class WhenResourceInfoNotFoundSpecs
{
    [Test]
    public async Task Execute_WhenResourceNotFound_ReturnsExitCode1()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = handler.Execute("unknown", resources, output, error);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_WhenResourceNotFound_WritesErrorToStderr()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute("unknown", resources, output, error);

        var errorOutput = error.ToString();
        await Assert.That(errorOutput).Contains("Error:");
        await Assert.That(errorOutput).Contains("'unknown'");
        await Assert.That(errorOutput).Contains("not found");
    }

    [Test]
    public async Task Execute_WhenResourceNotFound_DoesNotWriteToStdout()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute("unknown", resources, output, error);

        await Assert.That(output.ToString()).IsEmpty();
    }
}

/// <summary>
/// Tests for validation of the resource ID parameter.
/// </summary>
public class WhenResourceIdIsInvalidSpecs
{
    [Test]
    public async Task Execute_WhenIdIsEmpty_ReturnsExitCode1()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = handler.Execute(string.Empty, resources, output, error);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_WhenIdIsEmpty_WritesErrorToStderr()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute(string.Empty, resources, output, error);

        var errorOutput = error.ToString();
        await Assert.That(errorOutput).Contains("Error:");
        await Assert.That(errorOutput).Contains("required");
    }

    [Test]
    public async Task Execute_WhenIdIsWhitespace_ReturnsExitCode1()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = handler.Execute("   ", resources, output, error);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_WhenIdIsWhitespace_WritesErrorToStderr()
    {
        var handler = new ResourceInfoHandler();
        var resources = GlobalSharedResources.Empty;
        using var output = new StringWriter();
        using var error = new StringWriter();

        handler.Execute("   ", resources, output, error);

        var errorOutput = error.ToString();
        await Assert.That(errorOutput).Contains("Error:");
        await Assert.That(errorOutput).Contains("required");
    }
}