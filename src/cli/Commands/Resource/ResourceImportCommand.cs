using System.CommandLine;

using Spire.Cli.Services;
using Spire.Cli.Services.Configuration;

namespace Spire.Cli;

/// <summary>
/// Command to import shared resources from .aspire-shared-resources.json files in the current git repository.
/// </summary>
public sealed class ResourceImportCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "import";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Import shared resources from .aspire/settings.json files in the current git repository";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceImportCommand"/> class.
    /// </summary>
    public ResourceImportCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(CommonOptions.Yes);
    }

    private static async Task ImportAsync(
        ImportSharedResourcesRequest request,
        ISharedResourcesWriter writer,
        IGitService gitService)
    {
    }

    private readonly record struct ImportSharedResourcesRequest
    {
        public required string RepositoryPath { get; init; }
        public required bool AutoCloneExternalResources { get; init; }
    }
}
