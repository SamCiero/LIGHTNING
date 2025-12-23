namespace LIGHTNING.Core.Config;

/// <summary>
/// Represents the persisted configuration for LIGHTNING.  The config file is stored
/// under <see cref="IAppPaths.ConfigDirectory"/> and is serialized to/from YAML.
/// </summary>
public sealed record LightningConfig
{
    /// <summary>
    /// Configuration version used to assist in migrations.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>Root directory containing .met files to convert.</summary>
    public string MetSourceDir { get; init; } = string.Empty;

    /// <summary>Root directory of the active AF repository.</summary>
    public string AfRepoDir { get; init; } = string.Empty;

    /// <summary>Directory used to write temporary and intermediate files.</summary>
    public string AppWorkDir { get; init; } = string.Empty;

    /// <summary>
    /// Flag controlling optional VS Code integration.  Defaults to <c>false</c> and must be
    /// explicitly enabled by the user.  Future optional integrations can be added as
    /// additional boolean flags on this record.
    /// </summary>
    public bool EnableVsCodeIntegration { get; init; } = false;
}
