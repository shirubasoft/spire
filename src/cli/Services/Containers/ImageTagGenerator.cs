namespace Spire.Cli.Services;

/// <summary>
/// Generates image tags based on Git repository state.
/// </summary>
public sealed class ImageTagGenerator : IImageTagGenerator
{
    private const int ShortHashLength = 7;
    private const string DirtySuffix = "-dirty";

    private readonly IBranchNameSanitizer _branchNameSanitizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageTagGenerator"/> class.
    /// </summary>
    /// <param name="branchNameSanitizer">The branch name sanitizer.</param>
    public ImageTagGenerator(IBranchNameSanitizer branchNameSanitizer)
    {
        _branchNameSanitizer = branchNameSanitizer;
    }

    /// <inheritdoc />
    public ImageTags Generate(GitRepository repository)
    {
        var suffix = repository.IsDirty ? DirtySuffix : string.Empty;

        var commitHash = repository.LatestCommitHash.Length >= ShortHashLength
            ? repository.LatestCommitHash[..ShortHashLength]
            : repository.LatestCommitHash;

        return new ImageTags
        {
            CommitTag = commitHash + suffix,
            BranchTag = _branchNameSanitizer.Sanitize(repository.CurrentBranch) + suffix
        };
    }
}