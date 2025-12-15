using System;
using System.IO;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.Core.Policies;
using Xunit;

namespace LIGHTNING.Tests.Adapters.Filesystem;

public sealed class MutableBoundaryFileSystemProviderTests
{
    [Fact]
    public void SetCurrent_Swaps_Current_Instance()
    {
        string a = MakeTempDir();
        string b = MakeTempDir();

        try
        {
            var fsA = new BoundaryFileSystem(new BoundaryPolicy(new[] { a }));
            var fsB = new BoundaryFileSystem(new BoundaryPolicy(new[] { b }));

            var provider = new MutableBoundaryFileSystemProvider(fsA);

            Assert.Same(fsA, provider.Current);

            provider.SetCurrent(fsB);

            Assert.Same(fsB, provider.Current);
        }
        finally
        {
            TryDelete(a);
            TryDelete(b);
        }
    }

    private static string MakeTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "LIGHTNING_TEST_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch { }
    }
}
