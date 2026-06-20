using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Declaration;

namespace OpenSolarMax.Game.Modding;

internal record LevelModContext : IDisposable
{
    public required LevelModInfo Metadata { get; init; }

    public required ImmutableArray<BehaviorMod> BehaviorMods { get; init; }

    public required ImmutableArray<ContentMod> ContentMods { get; init; }

    public required IAssetsManager LocalAssets { get; init; }

    public required IConfigurationRoot LocalConfigs { get; init; }

    public required ImmutableArray<Type> ComponentTypes { get; init; }

    public required ImmutableDictionary<
        string,
        DeclarationSchemaInfo
    > DeclarationSchemaInfos { get; init; }

    public required BakedBehaviorsInfo GameplayBehaviors { get; init; }

    public required BakedBehaviorsInfo PreviewBehaviors { get; init; }

    public void Dispose()
    {
        // 释放资产缓存
        LocalAssets.Dispose();

        // 释放模组自身信息
        foreach (var mod in BehaviorMods)
            mod.Dispose();
        foreach (var mod in ContentMods)
            mod.Dispose();
    }
}
