using System;

namespace LIGHTNING.Adapters.Filesystem;

public sealed class MutableBoundaryFileSystemProvider : IMutableBoundaryFileSystemProvider
{
    private BoundaryFileSystem _current;

    public MutableBoundaryFileSystemProvider(BoundaryFileSystem initial)
    {
        _current = initial ?? throw new ArgumentNullException(nameof(initial));
    }

    public BoundaryFileSystem Current => _current;

    public void SetCurrent(BoundaryFileSystem fs)
    {
        _current = fs ?? throw new ArgumentNullException(nameof(fs));
    }
}
