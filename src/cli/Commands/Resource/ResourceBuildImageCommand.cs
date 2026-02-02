using System;
using System.Collections;
using System.CommandLine;

using Spire.Cli.Services;

namespace Spire.Cli.Commands.Resource;

/// <summary>
/// Command to build a container image for a shared resource.
/// </summary>
public sealed class ResourceBuildImageCommand : Command
{
    /// <summary>
    /// The name of the command.
    /// </summary>
    public const string CommandName = "build-image";

    /// <summary>
    /// The description of the command.
    /// </summary>
    public const string CommandDescription = "Builds a container image for a shared resource.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceBuildImageCommand"/> class.
    /// </summary>
    public ResourceBuildImageCommand() : base(CommandName, CommandDescription)
    {
        Options.Add(ResourceOptions.Ids);
        Options.Add(CommonOptions.Force);
    }

    private static ImageTags GetImageTags(GitRepository repository)
    {
        string suffix = repository.IsDirty ? "-dirty" : string.Empty;

        return new ImageTags
        {
            BranchTag = repository.CurrentBranch.ToLowerInvariant().Replace('/', '-') + suffix,
            CommitTag = repository.LatestCommitHash[..7] + suffix,
        };
    }

    private readonly record struct ImageTags : IEnumerable<string>
    {
        public required string BranchTag { get; init; }

        public required string CommitTag { get; init; }

        public IEnumerator<string> GetEnumerator()
        {
            yield return BranchTag;
            yield return CommitTag;
            yield return "latest";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
