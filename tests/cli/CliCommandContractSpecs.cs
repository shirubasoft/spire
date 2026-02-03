using System.Text.Json;

using CliWrap;
using CliWrap.Buffered;

namespace Spire.Cli.Tests;

/// <summary>
/// Integration tests that verify CLI commands exist and produce valid output.
/// These tests catch issues like build targets referencing non-existent CLI arguments.
/// </summary>
public class CliCommandContractSpecs
{
    [Test]
    public async Task ResourceListCommand_Exists()
    {
        // Act
        var result = await RunSpireCommandAsync("resource", "list --help");

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ResourceListCommand_ProducesValidJson()
    {
        // Act
        var result = await RunSpireCommandAsync("resource", "list");

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(() => JsonDocument.Parse(result.StandardOutput)).ThrowsNothing();
    }

    [Test]
    public async Task ResourceListCommand_OutputHasResourcesProperty()
    {
        // Act
        var result = await RunSpireCommandAsync("resource", "list");

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);

        using var doc = JsonDocument.Parse(result.StandardOutput);
        await Assert.That(doc.RootElement.TryGetProperty("resources", out _)).IsTrue();
    }

    [Test]
    public async Task ResourceInfoCommand_Exists()
    {
        // Act
        var result = await RunSpireCommandAsync("resource", "info --help");

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ModesCommand_Exists()
    {
        // Act
        var result = await RunSpireCommandAsync("modes", "--help");

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task OverrideCommand_Exists()
    {
        // Act
        var result = await RunSpireCommandAsync("override", "--help");

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    private static async Task<BufferedCommandResult> RunSpireCommandAsync(string command, string arguments)
    {
        return await CliWrap.Cli.Wrap("spire")
            .WithArguments($"{command} {arguments}")
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
    }
}
