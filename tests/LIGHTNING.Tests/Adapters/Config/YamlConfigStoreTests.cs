using System;
using System.IO;
using LIGHTNING.Adapters.Config;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.Core.Config;
using LIGHTNING.Core.Policies;
using LIGHTNING.Core.Services;
using Xunit;

namespace LIGHTNING.Tests.Adapters.Config;

public sealed class YamlConfigStoreTests
{
    [Fact]
    public void Save_Then_TryLoad_RoundTrips()
    {
        string appDataRoot = MakeTempDir();

        try
        {
            IAppPaths paths = new TestAppPaths(appDataRoot);

            var policy = new BoundaryPolicy(new[] { paths.ConfigDirectory });
            var fs = new BoundaryFileSystem(policy);
            var provider = new MutableBoundaryFileSystemProvider(fs);

            var store = new YamlConfigStore(provider, paths);

            var cfg = new LightningConfig
            {
                Version = 1,
                MetSourceDir = MakeTempDir(),
                AfRepoDir = MakeTempDir(),
                AppWorkDir = MakeTempDir()
            };

            store.Save(cfg);
            var loaded = store.TryLoad();

            Assert.NotNull(loaded);
            Assert.Equal(cfg.MetSourceDir, loaded!.MetSourceDir);
            Assert.Equal(cfg.AfRepoDir, loaded.AfRepoDir);
            Assert.Equal(cfg.AppWorkDir, loaded.AppWorkDir);
        }
        finally
        {
            TryDelete(appDataRoot);
        }
    }

    private sealed class TestAppPaths : IAppPaths
    {
        public TestAppPaths(string root)
        {
            ConfigDirectory = Path.Combine(root, "EmpyreanCodex", "LIGHTNING");
            ConfigPath = Path.Combine(ConfigDirectory, "config.yml");
        }

        public string ConfigDirectory { get; }
        public string ConfigPath { get; }
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
