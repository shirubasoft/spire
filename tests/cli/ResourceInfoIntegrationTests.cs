using System.CommandLine;
using Spire.Cli.Tests.TestHelpers;

namespace Spire.Cli.Tests;

/// <summary>
/// Integration tests for the resource info command with an existing resource.
/// </summary>
[NotInParallel("ConfigFile")]
public class InfoWithExistingResourceSpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();

        var resources = ConfigurationTestHelper.CreateConfig(
            ("postgres", ConfigurationTestHelper.CreateSampleResource("postgres"))
        );
        ConfigurationTestHelper.WriteConfig(resources);
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task Info_WithExistingResource_ReturnsExitCodeZero()
    {
        var command = new ResourceInfoCommand();
        var exitCode = await command.Parse(["--id", "postgres"]).InvokeAsync();

        await Assert.That(exitCode).IsEqualTo(0);
    }
}

/// <summary>
/// Integration tests for the resource info command with a non-existing resource.
/// </summary>
[NotInParallel("ConfigFile")]
public class InfoWithNonExistingResourceSpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();
        ConfigurationTestHelper.DeleteConfig();
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task Info_WithNonExistingResource_ReturnsExitCode1()
    {
        var command = new ResourceInfoCommand();
        var exitCode = await command.Parse(["--id", "unknown"]).InvokeAsync();

        await Assert.That(exitCode).IsEqualTo(1);
    }
}
