using System;
using System.IO;

namespace LIGHTNING.Core.Filesystem;

public static class PathUtil
{
    public static string Canonicalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is null/empty.", nameof(path));

        // Normalizes relative segments; does NOT resolve reparse points.
        var full = Path.GetFullPath(path);

        // Normalize trailing separators (keep root like C:\ intact).
        full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (full.EndsWith(":", StringComparison.Ordinal)) full += Path.DirectorySeparatorChar;

        return full;
    }

    public static bool IsSameOrDescendant(string candidateFullPath, string rootFullPath)
    {
        var candidate = Canonicalize(candidateFullPath);
        var root = Canonicalize(rootFullPath);

        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (string.Equals(candidate, root, cmp))
            return true;

        if (!root.EndsWith(Path.DirectorySeparatorChar))
            root += Path.DirectorySeparatorChar;

        return candidate.StartsWith(root, cmp);
    }
}
