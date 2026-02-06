namespace Spire.Hosting.Tests.AutoInstall;

public class AutoInstallDisabledByDefaultSpecs
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = MsBuildTestHelper.CreateTempDirectory();
    }

    [After(Test)]
    public void Cleanup()
    {
        MsBuildTestHelper.CleanupTempDirectory(_tempDir);
    }

    [Test]
    public async Task Target_DoesNotRun_WhenPropertyNotSet()
    {
        var (_, output) = await MsBuildTestHelper.RunMsBuildAsync(_tempDir);

        await Assert.That(output).DoesNotContain("Ensuring the 'spire' CLI tool is installed");
    }

    [Test]
    public async Task Target_DoesNotRun_WhenPropertySetToFalse()
    {
        var properties = new Dictionary<string, string> { ["SpireAutoInstallCli"] = "false" };

        var (_, output) = await MsBuildTestHelper.RunMsBuildAsync(_tempDir, properties);

        await Assert.That(output).DoesNotContain("Ensuring the 'spire' CLI tool is installed");
    }
}
