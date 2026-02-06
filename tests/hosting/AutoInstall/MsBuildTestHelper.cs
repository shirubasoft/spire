using System.Diagnostics;

namespace Spire.Hosting.Tests.AutoInstall;

internal static class MsBuildTestHelper
{
    private static readonly string HostingBuildDir = Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "..", "..", "src", "hosting", "build"));

    public static async Task<(int ExitCode, string Output)> RunMsBuildAsync(
        string tempDir,
        Dictionary<string, string>? properties = null,
        string target = "_EnsureSpireCliInstalled")
    {
        var propsPath = Path.Combine(HostingBuildDir, "Spire.Hosting.props");
        var targetsPath = Path.Combine(HostingBuildDir, "Spire.Hosting.targets");

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <SkipSharedResourceResolution>true</SkipSharedResourceResolution>
              </PropertyGroup>
              <Import Project="{propsPath}" />
              <Import Project="{targetsPath}" />
            </Project>
            """;

        var csprojPath = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csprojPath, csproj);

        var args = $"msbuild \"{csprojPath}\" /t:{target} /nologo /v:normal";
        if (properties is not null)
        {
            foreach (var (key, value) in properties)
            {
                args += $" /p:{key}={value}";
            }
        }

        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = tempDir
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout + stderr);
    }

    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "spire-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void CleanupTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
