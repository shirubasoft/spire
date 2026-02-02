using System.CommandLine;

namespace Spire.Cli;

/// <summary>
/// Command to show detailed information about a shared resource.
/// </summary>
public sealed class ResourceInfoCommand : Command
{
    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "info";

    /// <summary>
    /// The command description.
    /// </summary>
    public const string CommandDescription = "Show detailed information about a shared resource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceInfoCommand"/> class.
    /// </summary>
    public ResourceInfoCommand() : base(name: CommandName, description: CommandDescription)
    {
        Options.Add(ResourceOptions.Id);
        Options.Add(CommonOptions.Level);
    }

    private static async Task<SharedResource> GetSharedResource(GetSharedResourceRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private readonly record struct GetSharedResourceRequest
    {
        public required string Id { get; init; }

        public required Level Level { get; init; }
    }
}
