namespace LIGHTNING.Core.Policies;

public interface IBoundaryPolicy
{
    bool IsAllowedPath(string fullPath);
}
