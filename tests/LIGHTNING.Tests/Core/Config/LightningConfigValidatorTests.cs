using System;
using System.IO;
using LIGHTNING.Core.Config;
using Xunit;

namespace LIGHTNING.Tests.Core.Config;

public sealed class LightningConfigValidatorTests
{
    [Fact]
    public void Validate_When_Dirs_Exist_And_Do_Not_Overlap_Is_Valid()
    {
        string a = MakeTempDir();
        string b = MakeTempDir();
        string c = MakeTempDir();

        try
        {
            var cfg = new LightningConfig
            {
                Version = 1,
                MetSourceDir = a,
                AfRepoDir = b,
                AppWorkDir = c
            };

            var errors = LightningConfigValidator.Validate(cfg);
            Assert.Empty(errors);
        }
        finally
        {
            TryDelete(a);
            TryDelete(b);
            TryDelete(c);
        }
    }

    [Fact]
    public void Validate_When_Dirs_Overlap_Is_Invalid()
    {
        string parent = MakeTempDir();
        string child = Path.Combine(parent, "child");
        Directory.CreateDirectory(child);

        string other = MakeTempDir();

        try
        {
            var cfg = new LightningConfig
            {
                Version = 1,
                MetSourceDir = parent,
                AfRepoDir = child,
                AppWorkDir = other
            };

            var errors = LightningConfigValidator.Validate(cfg);
            Assert.NotEmpty(errors);
        }
        finally
        {
            TryDelete(parent);
            TryDelete(other);
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
        catch
        {
            // best-effort cleanup for test temp dirs
        }
    }
}
