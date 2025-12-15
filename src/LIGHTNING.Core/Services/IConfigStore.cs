using LIGHTNING.Core.Config;

namespace LIGHTNING.Core.Services;

public interface IConfigStore
{
    LightningConfig? TryLoad();
    void Save(LightningConfig config);
}
