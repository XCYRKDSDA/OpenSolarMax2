using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.System;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据相对变换<see cref="RelativeTransform"/>及其树型关系计算每个实体的绝对变换
/// </summary>
/// <param name="world"></param>
public sealed partial class CalculateAbsoluteTransformSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), IUpdateSystem
{
    private static void RecursivelyUpdateAbsoluteTransform(in Entity entity, in Matrix parentTransformToRoot)
    {
        ref readonly var relativeTransform = ref entity.Get<RelativeTransform>();
        var transformToRoot = relativeTransform.TransformToParent * parentTransformToRoot;

        // 计算该实体相对默认根实体的绝对变换
        entity.Get<AbsoluteTransform>().TransformToRoot = transformToRoot;

        // 递归考察子实体
        foreach (var child in entity.GetChildren<RelativeTransform>())
            RecursivelyUpdateAbsoluteTransform(in child, in transformToRoot);
    }

    [Query]
    [All(typeof(TreeRelationship<RelativeTransform>), typeof(RelativeTransform), typeof(AbsoluteTransform))]
    private static void UpdateAbsoluteTransform(in TreeRelationship<RelativeTransform> relationship,
                                                in RelativeTransform relativeTransform,
                                                ref AbsoluteTransform absoluteTransform)
    {
        if (relationship.Parent != Entity.Null)
            return;

        absoluteTransform.TransformToRoot = relativeTransform.TransformToParent;
        foreach (var child in relationship.Children)
            RecursivelyUpdateAbsoluteTransform(in child, absoluteTransform.TransformToRoot);
    }
}
