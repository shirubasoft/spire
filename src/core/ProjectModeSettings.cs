namespace Spire;

/// <summary>
/// Configuration for running a shared resource in project mode.
/// </summary>
public sealed record ProjectModeSettings
{
    /// <summary>
    /// The path to the project file (.csproj).
    /// </summary>
    public required string ProjectPath { get; init; }
}