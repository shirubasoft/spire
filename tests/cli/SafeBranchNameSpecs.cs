using Spire.Cli.Services;

namespace Spire.Cli.Tests;

/// <summary>
/// Tests for branch name sanitization for use as container image tags.
/// </summary>
public class SimpleBranchSpecs
{
    [Test]
    public async Task Sanitize_SimpleBranch_Unchanged()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("main");

        await Assert.That(result).IsEqualTo("main");
    }
}

/// <summary>
/// Tests for branch names containing slashes.
/// </summary>
public class BranchWithSlashSpecs
{
    [Test]
    public async Task Sanitize_WithSlash_ReplacesWithDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("feature/x");

        await Assert.That(result).IsEqualTo("feature-x");
    }

    [Test]
    public async Task Sanitize_FeatureAddAuth_ReplacesSlashWithDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("feature/add-auth");

        await Assert.That(result).IsEqualTo("feature-add-auth");
    }

    [Test]
    public async Task Sanitize_ReleaseVersion_ReplacesSlashWithDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("release/v1.0.0");

        await Assert.That(result).IsEqualTo("release-v1.0.0");
    }
}

/// <summary>
/// Tests for branch names with uppercase letters.
/// </summary>
public class UppercaseBranchSpecs
{
    [Test]
    public async Task Sanitize_Uppercase_Lowercases()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("Feature");

        await Assert.That(result).IsEqualTo("feature");
    }

    [Test]
    public async Task Sanitize_MixedCase_Lowercases()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("Feature/Add_Auth");

        await Assert.That(result).IsEqualTo("feature-add-auth");
    }
}

/// <summary>
/// Tests for branch names containing underscores.
/// </summary>
public class BranchWithUnderscoreSpecs
{
    [Test]
    public async Task Sanitize_WithUnderscore_ReplacesWithDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("add_feature");

        await Assert.That(result).IsEqualTo("add-feature");
    }
}

/// <summary>
/// Tests for branch names with multiple invalid characters.
/// </summary>
public class ComplexBranchNameSpecs
{
    [Test]
    public async Task Sanitize_MultipleInvalid_ReplacesAll()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("hotfix/BUG-123");

        await Assert.That(result).IsEqualTo("hotfix-bug-123");
    }

    [Test]
    public async Task Sanitize_MultipleSlashesAndUnderscores_ReplacesAll()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("feature/sub_module/task");

        await Assert.That(result).IsEqualTo("feature-sub-module-task");
    }
}

/// <summary>
/// Tests for branch names with leading or trailing dashes.
/// </summary>
public class LeadingTrailingDashSpecs
{
    [Test]
    public async Task Sanitize_LeadingSlash_TrimsDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("/leading");

        await Assert.That(result).IsEqualTo("leading");
    }

    [Test]
    public async Task Sanitize_TrailingSlash_TrimsDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("trailing/");

        await Assert.That(result).IsEqualTo("trailing");
    }

    [Test]
    public async Task Sanitize_LeadingAndTrailingSlash_TrimsBothDashes()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("/both/");

        await Assert.That(result).IsEqualTo("both");
    }

    [Test]
    public async Task Sanitize_ConsecutiveSlashes_CollapsesToSingleDash()
    {
        var sanitizer = new BranchNameSanitizer();

        var result = sanitizer.Sanitize("feature//test");

        await Assert.That(result).IsEqualTo("feature-test");
    }
}