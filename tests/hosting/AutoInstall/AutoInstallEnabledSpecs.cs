namespace Spire.Hosting.Tests.AutoInstall;

public class AutoInstallEnabledSpecs
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
    public async Task Target_Runs_WhenPropertySetToTrue()
    {
        var properties = new Dictionary<string, string>
        {
            ["SpireAutoInstallCli"] = "true",
            ["SkipSharedResourceResolution"] = "false"
        };

        var (_, output) = await MsBuildTestHelper.RunMsBuildAsync(_tempDir, properties);

        await Assert.That(output).Contains("Ensuring the 'spire' CLI tool is installed");
    }
}
