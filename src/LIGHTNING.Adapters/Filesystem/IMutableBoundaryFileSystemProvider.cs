namespace LIGHTNING.Adapters.Filesystem;

public interface IMutableBoundaryFileSystemProvider : IBoundaryFileSystemProvider
{
    void SetCurrent(BoundaryFileSystem fs);
}
