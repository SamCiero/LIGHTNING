using System.IO;
using LIGHTNING.Core.Config;
using LIGHTNING.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LIGHTNING.Adapters.Config;

public sealed class YamlConfigStore : IConfigStore
{
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

    public LightningConfig? TryLoad()
    {
        var path = AppPaths.ConfigPath;
        if (!File.Exists(path)) return null;

        var yaml = File.ReadAllText(path);
        return Deserializer.Deserialize<LightningConfig>(yaml);
    }

    public void Save(LightningConfig config)
    {
        Directory.CreateDirectory(AppPaths.ConfigDirectory);
        var yaml = Serializer.Serialize(config);
        File.WriteAllText(AppPaths.ConfigPath, yaml);
    }
}
