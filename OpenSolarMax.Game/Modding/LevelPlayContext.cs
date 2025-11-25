using System.Reflection;
using Nine.Assets;

namespace OpenSolarMax.Game.Modding;

internal class LevelPlayContext
{
    public required ILevelMod LevelMod { get; init; }

    public required IBehaviorMod[] BehaviorMods { get; init; }

    public required IContentMod[] ContentMods { get; init; }

    public required Assembly[] Assemblies { get; init; }

    public required IAssetsManager LocalAssets { get; init; }

    public required Dictionary<string, Type[]> ConfigurationTypes { get; init; }

    public required SystemTypeCollection SystemTypes { get; init; }

    public required Dictionary<string, MethodInfo[]> HookImplMethods { get; init; }
}
