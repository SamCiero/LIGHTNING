using System;
using System.IO;

namespace LIGHTNING.Core.Services;

public static class AppPaths
{
    // Matches README intent: LocalAppData\EmpyreanCodex\LIGHTNING\config.yml
    public static string ConfigDirectory =>
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EmpyreanCodex", "LIGHTNING");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.yml");
}
