using System;
using System.Collections.Generic;
using LIGHTNING.Core.Filesystem;

namespace LIGHTNING.Core.Policies;

public sealed class BoundaryPolicy : IBoundaryPolicy
{
    private readonly string[] _allowedRootsFull;

    public BoundaryPolicy(IEnumerable<string> allowedRoots)
    {
        if (allowedRoots is null) throw new ArgumentNullException(nameof(allowedRoots));
        var list = new List<string>();
        foreach (var r in allowedRoots)
            list.Add(PathUtil.Canonicalize(r));
        _allowedRootsFull = list.ToArray();
    }

    public bool IsAllowedPath(string fullPath)
    {
        foreach (var root in _allowedRootsFull)
            if (PathUtil.IsSameOrDescendant(fullPath, root))
                return true;

        return false;
    }
}
