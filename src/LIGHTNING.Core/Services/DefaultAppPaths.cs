using System;
using System.IO;

namespace LIGHTNING.Core.Services;

public sealed class DefaultAppPaths : IAppPaths
{
    public string ConfigDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EmpyreanCodex",
            "LIGHTNING");

    public string ConfigPath => Path.Combine(ConfigDirectory, "config.yml");
}
