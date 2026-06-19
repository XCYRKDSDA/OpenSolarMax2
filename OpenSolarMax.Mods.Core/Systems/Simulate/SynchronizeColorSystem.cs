using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 递归遍历树结构，将父实体的 Sprite.Color 复制到所有子实体
/// </summary>
[SimulateSystem, AfterStructuralChanges, BothForGameplayAndPreview]
[
    ReadCurr(typeof(TreeRelationship<ColorSync>.AsParent)),
    ReadCurr(typeof(TreeRelationship<ColorSync>.AsChild)),
    Iterate(typeof(Sprite))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class SynchronizeColorSystem(World world) : ICalcSystem
{
    private static void RecursivelySyncColor(Entity entity)
    {
        // 如果实体不再是颜色同步关系的父实体, 就此停止
        if (!entity.TryGet<TreeRelationship<ColorSync>.AsParent>(out var asParent))
            return;
        var parentColor = entity.Get<Sprite>().Color;

        // 遍历所有子实体，同步颜色
        foreach (var (_, record) in asParent.Relationships)
        {
            ref var childSprite = ref record.Child.Get<Sprite>();
            childSprite.Color = parentColor;
        }

        // 递归处理子实体的子实体
        foreach (var (_, record) in asParent.Relationships)
            RecursivelySyncColor(record.Child);
    }

    [Query]
    [All<TreeRelationship<ColorSync>.AsParent, Sprite>]
    private static void SyncFromRoot(Entity root)
    {
        // 如果该实体以子身份参与关系，则不是根实体，需要跳过
        if (
            root.TryGet<TreeRelationship<ColorSync>.AsChild>(out var asChild)
            && asChild.Relationship is not null
        )
            return;

        RecursivelySyncColor(root);
    }

    public void Update() => SyncFromRootQuery(world);
}
