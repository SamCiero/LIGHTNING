namespace LIGHTNING.Adapters.Filesystem;

public interface IBoundaryFileSystemProvider
{
    BoundaryFileSystem Current { get; }
}
