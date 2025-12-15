using System;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.Core.Config;
using LIGHTNING.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LIGHTNING.Adapters.Config;

public sealed class YamlConfigStore : IConfigStore
{
    private readonly IBoundaryFileSystemProvider _fs;
    private readonly IAppPaths _paths;

    private static IDeserializer Deserializer { get; } =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private static ISerializer Serializer { get; } =
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

    public YamlConfigStore(IBoundaryFileSystemProvider fs, IAppPaths paths)
    {
        _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public LightningConfig? TryLoad()
    {
        if (!_fs.Current.FileExists(_paths.ConfigPath))
            return null;

        string yaml = _fs.Current.ReadAllText(_paths.ConfigPath);
        return Deserializer.Deserialize<LightningConfig>(yaml);
    }

    public void Save(LightningConfig config)
    {
        _fs.Current.EnsureDirectoryExists(_paths.ConfigDirectory);

        string yaml = Serializer.Serialize(config);
        _fs.Current.WriteAllText(_paths.ConfigPath, yaml);
    }
}
