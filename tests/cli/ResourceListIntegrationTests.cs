using System.CommandLine;

using Spire.Cli.Tests.TestHelpers;

namespace Spire.Cli.Tests;

/// <summary>
/// Integration tests for the resource list command with an empty configuration.
/// </summary>
[NotInParallel("ConfigFile")]
public class ListWithEmptyConfigSpecs
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
    public async Task List_WithEmptyConfig_ReturnsExitCodeZero()
    {
        var command = new ResourceListCommand();
        var exitCode = await command.Parse([]).InvokeAsync();

        await Assert.That(exitCode).IsEqualTo(0);
    }
}

/// <summary>
/// Integration tests for the resource list command with populated configuration.
/// </summary>
[NotInParallel("ConfigFile")]
public class ListWithPopulatedConfigSpecs
{
    private bool _hadBackup;

    [Before(Test)]
    public void Setup()
    {
        _hadBackup = ConfigurationTestHelper.BackupConfig();

        var resources = ConfigurationTestHelper.CreateConfig(
            ("postgres", ConfigurationTestHelper.CreateSampleResource("postgres")),
            ("redis", ConfigurationTestHelper.CreateSampleResource("redis"))
        );
        ConfigurationTestHelper.WriteConfig(resources);
    }

    [After(Test)]
    public void Cleanup()
    {
        ConfigurationTestHelper.RestoreConfig(_hadBackup);
    }

    [Test]
    public async Task List_WithPopulatedConfig_ReturnsExitCodeZero()
    {
        var command = new ResourceListCommand();
        var exitCode = await command.Parse([]).InvokeAsync();

        await Assert.That(exitCode).IsEqualTo(0);
    }
}