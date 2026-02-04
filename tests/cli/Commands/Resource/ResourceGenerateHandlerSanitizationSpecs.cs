using Spire.Cli.Commands.Resource.Handlers;

namespace Spire.Cli.Tests.Commands.Resource;

/// <summary>
/// Tests for ResourceGenerateHandler.SanitizeResourceId method.
/// Ensures resource IDs and image names only contain lowercase letters, numbers, and dashes.
/// </summary>
public sealed class ResourceIdSanitizationSpecs
{
    [Test]
    public async Task SanitizeResourceId_WithDots_ReplacesDashesWithDashes()
    {
        // Arrange
        const string input = "sample.web";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("sample-web");
    }

    [Test]
    public async Task SanitizeResourceId_WithMultipleDots_ReplacesAllWithDashes()
    {
        // Arrange
        const string input = "my.company.api.service";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("my-company-api-service");
    }

    [Test]
    public async Task SanitizeResourceId_WithUppercase_ConvertsToLowercase()
    {
        // Arrange
        const string input = "MyService";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("myservice");
    }

    [Test]
    public async Task SanitizeResourceId_WithUnderscores_ReplacesWithDashes()
    {
        // Arrange
        const string input = "my_service_api";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("my-service-api");
    }

    [Test]
    public async Task SanitizeResourceId_WithSpaces_ReplacesWithDashes()
    {
        // Arrange
        const string input = "my service";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("my-service");
    }

    [Test]
    public async Task SanitizeResourceId_WithSpecialCharacters_ReplacesWithDashes()
    {
        // Arrange
        const string input = "my@service#api$v1";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("my-service-api-v1");
    }

    [Test]
    public async Task SanitizeResourceId_WithMultipleConsecutiveSpecialChars_CollapsesToSingleDash()
    {
        // Arrange
        const string input = "my...service";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("my-service");
    }

    [Test]
    public async Task SanitizeResourceId_WithLeadingDots_RemovesLeadingDashes()
    {
        // Arrange
        const string input = "...service";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("service");
    }

    [Test]
    public async Task SanitizeResourceId_WithTrailingDots_RemovesTrailingDashes()
    {
        // Arrange
        const string input = "service...";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("service");
    }

    [Test]
    public async Task SanitizeResourceId_WithNumbers_PreservesNumbers()
    {
        // Arrange
        const string input = "api.v2.service";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("api-v2-service");
    }

    [Test]
    public async Task SanitizeResourceId_WithValidInput_ReturnsUnchanged()
    {
        // Arrange
        const string input = "my-service-api";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("my-service-api");
    }

    [Test]
    public async Task SanitizeResourceId_WithMixedCasing_NormalizesToLowercase()
    {
        // Arrange
        const string input = "Sample.Web";

        // Act
        var result = ResourceGenerateHandler.SanitizeResourceId(input);

        // Assert
        await Assert.That(result).IsEqualTo("sample-web");
    }
}
