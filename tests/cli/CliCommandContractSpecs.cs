using System.Diagnostics;
using System.Text.Json;

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
        var (exitCode, _, _) = await RunSpireCommandAsync("resource list --help");

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ResourceListCommand_ProducesValidJson()
    {
        // Act
        var (exitCode, stdout, _) = await RunSpireCommandAsync("resource list");

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(() => JsonDocument.Parse(stdout)).ThrowsNothing();
    }

    [Test]
    public async Task ResourceListCommand_OutputHasResourcesProperty()
    {
        // Act
        var (exitCode, stdout, _) = await RunSpireCommandAsync("resource list");

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);

        using var doc = JsonDocument.Parse(stdout);
        await Assert.That(doc.RootElement.TryGetProperty("resources", out _)).IsTrue();
    }

    [Test]
    public async Task ResourceInfoCommand_Exists()
    {
        // Act
        var (exitCode, _, _) = await RunSpireCommandAsync("resource info --help");

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ModesCommand_Exists()
    {
        // Act
        var (exitCode, _, _) = await RunSpireCommandAsync("modes --help");

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task OverrideCommand_Exists()
    {
        // Act
        var (exitCode, _, _) = await RunSpireCommandAsync("override --help");

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunSpireCommandAsync(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "spire",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (process.ExitCode, stdout, stderr);
    }
}
