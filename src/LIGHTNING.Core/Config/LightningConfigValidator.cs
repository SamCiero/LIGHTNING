using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LIGHTNING.Core.Filesystem;

namespace LIGHTNING.Core.Config;

public static class LightningConfigValidator
{
    /// <summary>
    /// Validate the supplied configuration.  Returns a list of error messages; an
    /// empty list indicates success.  Validation ensures required fields are
    /// specified, paths are absolute local directories, roots do not overlap or share
    /// volumes, the MET source directory is flat, the AF repo contains a .git
    /// folder, and the work directory is writable/creatable.
    /// </summary>
    public static IReadOnlyList<string> Validate(LightningConfig cfg)
    {
        var errors = new List<string>();

        // Ensure required fields are present
        if (string.IsNullOrWhiteSpace(cfg.MetSourceDir)) errors.Add("MetSourceDir is required.");
        if (string.IsNullOrWhiteSpace(cfg.AfRepoDir)) errors.Add("AfRepoDir is required.");
        if (string.IsNullOrWhiteSpace(cfg.AppWorkDir)) errors.Add("AppWorkDir is required.");

        if (errors.Count > 0) return errors;

        // Canonicalize paths and ensure they are absolute/local (not UNC)
        string met = PathUtil.Canonicalize(cfg.MetSourceDir);
        string af = PathUtil.Canonicalize(cfg.AfRepoDir);
        string work = PathUtil.Canonicalize(cfg.AppWorkDir);

        if (!Path.IsPathFullyQualified(met) || met.StartsWith("\\"))
            errors.Add($"MetSourceDir must be an absolute local path: {met}");
        if (!Path.IsPathFullyQualified(af) || af.StartsWith("\\"))
            errors.Add($"AfRepoDir must be an absolute local path: {af}");
        if (!Path.IsPathFullyQualified(work) || work.StartsWith("\\"))
            errors.Add($"AppWorkDir must be an absolute local path: {work}");

        if (errors.Count > 0) return errors;

        // Existence checks for MET source and AF repo roots
        if (!Directory.Exists(met)) errors.Add($"MetSourceDir does not exist: {met}");
        if (!Directory.Exists(af)) errors.Add($"AfRepoDir does not exist: {af}");

        // Disallow overlap among the three roots
        if (PathUtil.IsSameOrDescendant(met, af) || PathUtil.IsSameOrDescendant(af, met))
            errors.Add("MetSourceDir and AfRepoDir must not overlap.");
        if (PathUtil.IsSameOrDescendant(met, work) || PathUtil.IsSameOrDescendant(work, met))
            errors.Add("MetSourceDir and AppWorkDir must not overlap.");
        if (PathUtil.IsSameOrDescendant(af, work) || PathUtil.IsSameOrDescendant(work, af))
            errors.Add("AfRepoDir and AppWorkDir must not overlap.");

        // Enforce distinct volumes (no two roots on the same drive)
        string? metRoot = Path.GetPathRoot(met);
        string? afRoot = Path.GetPathRoot(af);
        string? workRoot = Path.GetPathRoot(work);
        if (!string.IsNullOrWhiteSpace(metRoot) && string.Equals(metRoot, afRoot, StringComparison.OrdinalIgnoreCase))
            errors.Add("MetSourceDir and AfRepoDir must be on different volumes.");
        if (!string.IsNullOrWhiteSpace(metRoot) && string.Equals(metRoot, workRoot, StringComparison.OrdinalIgnoreCase))
            errors.Add("MetSourceDir and AppWorkDir must be on different volumes.");
        if (!string.IsNullOrWhiteSpace(afRoot) && string.Equals(afRoot, workRoot, StringComparison.OrdinalIgnoreCase))
            errors.Add("AfRepoDir and AppWorkDir must be on different volumes.");

        // Flatness requirement for MET source: no subdirectories
        if (Directory.Exists(met))
        {
            var subdirs = Directory.EnumerateDirectories(met)
                                    .Where(d => !IsHidden(d)).ToList();
            if (subdirs.Count > 0)
                errors.Add("MetSourceDir must be flat and contain no subdirectories.");
        }

        // AF repo must contain a .git directory
        if (Directory.Exists(af))
        {
            string gitPath = Path.Combine(af, ".git");
            if (!Directory.Exists(gitPath))
                errors.Add("AfRepoDir must contain a .git folder.");
        }

        // Check that work directory is creatable/writable
        try
        {
            if (!Directory.Exists(work)) Directory.CreateDirectory(work);
            string testFile = Path.Combine(work, ".write_test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            errors.Add($"AppWorkDir is not writable or creatable: {ex.Message}");
        }

        return errors;
    }

    private static bool IsHidden(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            return (attrs & (FileAttributes.Hidden | FileAttributes.System)) != 0;
        }
        catch
        {
            return false;
        }
    }
}
