using System.Text.RegularExpressions;

namespace Spire.Cli.Services;

/// <summary>
/// Sanitizes branch names for use as container image tags.
/// </summary>
public sealed partial class BranchNameSanitizer : IBranchNameSanitizer
{
    /// <inheritdoc />
    public string Sanitize(string branchName)
    {
        ArgumentNullException.ThrowIfNull(branchName);

        // Convert to lowercase
        var result = branchName.ToLowerInvariant();

        // Replace slashes and underscores with dashes
        result = InvalidCharsRegex().Replace(result, "-");

        // Remove any consecutive dashes
        result = ConsecutiveDashesRegex().Replace(result, "-");

        // Trim leading and trailing dashes
        result = result.Trim('-');

        return result;
    }

    [GeneratedRegex(@"[/_]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex ConsecutiveDashesRegex();
}