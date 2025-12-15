using System.Collections.Generic;
using System.IO;
using LIGHTNING.Core.Filesystem;

namespace LIGHTNING.Core.Config;

public static class LightningConfigValidator
{
    public static IReadOnlyList<string> Validate(LightningConfig cfg)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(cfg.MetSourceDir)) errors.Add("MetSourceDir is required.");
        if (string.IsNullOrWhiteSpace(cfg.AfRepoDir)) errors.Add("AfRepoDir is required.");
        if (string.IsNullOrWhiteSpace(cfg.AppWorkDir)) errors.Add("AppWorkDir is required.");

        if (errors.Count > 0) return errors;

        var met = PathUtil.Canonicalize(cfg.MetSourceDir);
        var af = PathUtil.Canonicalize(cfg.AfRepoDir);
        var work = PathUtil.Canonicalize(cfg.AppWorkDir);

        if (!Directory.Exists(met)) errors.Add($"MetSourceDir does not exist: {met}");
        if (!Directory.Exists(af)) errors.Add($"AfRepoDir does not exist: {af}");

        // Disallow overlap among the 3 roots
        if (PathUtil.IsSameOrDescendant(met, af) || PathUtil.IsSameOrDescendant(af, met))
            errors.Add("MetSourceDir and AfRepoDir must not overlap.");
        if (PathUtil.IsSameOrDescendant(met, work) || PathUtil.IsSameOrDescendant(work, met))
            errors.Add("MetSourceDir and AppWorkDir must not overlap.");
        if (PathUtil.IsSameOrDescendant(af, work) || PathUtil.IsSameOrDescendant(work, af))
            errors.Add("AfRepoDir and AppWorkDir must not overlap.");

        return errors;
    }
}
