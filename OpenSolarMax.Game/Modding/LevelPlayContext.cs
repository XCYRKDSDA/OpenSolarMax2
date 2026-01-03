using System.Reflection;
using Nine.Assets;

namespace OpenSolarMax.Game.Modding;

internal class LevelPlayContext
{
    public required ILevelModInfo LevelModInfo { get; init; }

    public required IBehaviorModInfo[] BehaviorModInfos { get; init; }

    public required IContentModInfo[] ContentModInfos { get; init; }

    public required Assembly[] Assemblies { get; init; }

    public required IAssetsManager LocalAssets { get; init; }

    public required Dictionary<string, Type[]> ConfigurationTypes { get; init; }

    public required SystemTypeCollection SystemTypes { get; init; }

    public required Dictionary<string, MethodInfo[]> HookImplMethods { get; init; }
}
