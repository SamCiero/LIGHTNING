using System;
using System.IO;
using LIGHTNING.Core.Exceptions;
using LIGHTNING.Core.Policies;

namespace LIGHTNING.Adapters.Filesystem;

public sealed class BoundaryFileSystem
{
    private readonly IBoundaryPolicy _boundary;

    public BoundaryFileSystem(IBoundaryPolicy boundary)
    {
        _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }

    public void EnsureAllowed(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is null/empty.", nameof(path));

        string full = Path.GetFullPath(path);

        if (!_boundary.IsAllowedPath(full))
            throw new BoundaryViolationException($"Path is outside allowed roots: {full}");

        ThrowIfReparsePointOnExistingAncestors(full);
    }

    public bool FileExists(string path)
    {
        EnsureAllowed(path);
        return File.Exists(path);
    }

    public string ReadAllText(string path)
    {
        EnsureAllowed(path);
        return File.ReadAllText(path);
    }

    public void EnsureDirectoryExists(string path)
    {
        EnsureAllowed(path);

        Directory.CreateDirectory(path);

        // After creation, ensure the directory itself isn't a reparse point.
        ThrowIfReparsePointOnExistingAncestors(Path.GetFullPath(path));
    }

    public void WriteAllText(string path, string contents)
    {
        EnsureAllowed(path);

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            EnsureDirectoryExists(dir);

        File.WriteAllText(path, contents);

        // If the file exists, check it isn't a reparse point either.
        if (File.Exists(path))
        {
            var attrs = File.GetAttributes(path);
            if ((attrs & FileAttributes.ReparsePoint) != 0)
                throw new BoundaryViolationException($"Reparse point blocked: {Path.GetFullPath(path)}");
        }
    }

    private static void ThrowIfReparsePointOnExistingAncestors(string fullPath)
    {
        // Check the nearest existing directory ancestor chain.
        string? cursor = Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);

        while (!string.IsNullOrWhiteSpace(cursor) && Directory.Exists(cursor))
        {
            var attrs = File.GetAttributes(cursor);
            if ((attrs & FileAttributes.ReparsePoint) != 0)
                throw new BoundaryViolationException($"Reparse point blocked: {cursor}");

            string? parent = Path.GetDirectoryName(cursor);
            if (string.IsNullOrWhiteSpace(parent) || parent == cursor)
                break;

            cursor = parent;
        }
    }

    public DateTime GetLastWriteTime(string path)
    {
        EnsureAllowed(path);
        return File.GetLastWriteTime(Path.GetFullPath(path));
    }

}
