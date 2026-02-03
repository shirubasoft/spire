namespace Spire.Cli.Tests;

public class PlaceholderTests
{
    [Test]
    public async Task Placeholder_ShouldPass()
    {
        var value = 1 + 1;
        await Assert.That(value).IsEqualTo(2);
    }
}
