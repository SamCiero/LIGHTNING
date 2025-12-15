namespace LIGHTNING.Core.Config;

public sealed record LightningConfig
{
    public int Version { get; init; } = 1;

    public string MetSourceDir { get; init; } = "";
    public string AfRepoDir { get; init; } = "";
    public string AppWorkDir { get; init; } = "";
}
