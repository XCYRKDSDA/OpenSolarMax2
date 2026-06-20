using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Declaration;

namespace OpenSolarMax.Game.Modding;

internal record LevelModContext(
    LevelModInfo Metadata,
    ImmutableArray<BehaviorMod> BehaviorMods,
    ImmutableArray<ContentMod> ContentMods,
    IAssetsManager LocalAssets,
    IConfigurationRoot LocalConfigs,
    ImmutableArray<Type> ComponentTypes,
    ImmutableDictionary<string, DeclarationSchemaInfo> DeclarationSchemaInfos,
    BakedBehaviorsInfo GameplayBehaviors,
    BakedBehaviorsInfo PreviewBehaviors
) : IDisposable
{
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
