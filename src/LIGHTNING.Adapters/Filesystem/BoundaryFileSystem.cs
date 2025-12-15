using System.IO;
using LIGHTNING.Core.Exceptions;
using LIGHTNING.Core.Policies;

namespace LIGHTNING.Adapters.Filesystem;

public sealed class BoundaryFileSystem
{
    private readonly IBoundaryPolicy _boundary;

    public BoundaryFileSystem(IBoundaryPolicy boundary)
    {
        _boundary = boundary;
    }

    public void EnsureAllowed(string path)
    {
        var full = Path.GetFullPath(path);
        if (!_boundary.IsAllowedPath(full))
            throw new BoundaryViolationException($"Path is outside allowed roots: {full}");

        // M0: reparse-point blocking will be implemented here next:
        // - walk parents; if any has FileAttributes.ReparsePoint => throw
    }
}
