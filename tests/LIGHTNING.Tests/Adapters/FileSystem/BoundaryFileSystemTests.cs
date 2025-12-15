using System;
using System.IO;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.Core.Exceptions;
using LIGHTNING.Core.Policies;
using Xunit;

namespace LIGHTNING.Tests.Adapters.FileSystem;

public sealed class BoundaryFileSystemTests
{
    [Fact]
    public void EnsureAllowed_Allows_Path_Under_Root()
    {
        string root = MakeTempDir();

        try
        {
            var fs = new BoundaryFileSystem(new BoundaryPolicy(new[] { root }));
            fs.EnsureAllowed(Path.Combine(root, "child.txt"));
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public void EnsureAllowed_Blocks_Path_Outside_Root()
    {
        string root = MakeTempDir();
        string outside = Path.Combine(Path.GetTempPath(), "outside_" + Guid.NewGuid().ToString("N"));

        try
        {
            var fs = new BoundaryFileSystem(new BoundaryPolicy(new[] { root }));
            Assert.Throws<BoundaryViolationException>(() => fs.EnsureAllowed(outside));
        }
        finally
        {
            TryDelete(root);
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
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
    }
}
