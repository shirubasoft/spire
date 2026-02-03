namespace Spire.Cli.Services;

/// <summary>
/// Sanitizes branch names for use as container image tags.
/// </summary>
public interface IBranchNameSanitizer
{
    /// <summary>
    /// Sanitizes a branch name for use as a container image tag.
    /// Converts to lowercase, replaces slashes and underscores with dashes,
    /// and removes leading/trailing dashes.
    /// </summary>
    /// <param name="branchName">The branch name to sanitize.</param>
    /// <returns>A sanitized branch name safe for use as an image tag.</returns>
    string Sanitize(string branchName);
}
