using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据相对变换<see cref="RelativeTransform"/>及其树型关系计算每个实体的绝对变换
/// </summary>
[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(IndexTransformTreeSystem))] //需要在更新完坐标变换树后再执行
public sealed partial class CalculateAbsoluteTransformSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private static void RecursivelyUpdateAbsoluteTransform(Entity entity)
    {
        var parentTransformToRoot = entity.Get<AbsoluteTransform>().TransformToRoot;

        // 先计算子实体的变换，感觉比递归的cache miss会少一些
        foreach (var (relationship, record) in entity.Get<TreeRelationship<RelativeTransform>.AsParent>().Relationships)
        {
            var transformToParent = relationship.Entity.Get<RelativeTransform>().TransformToParent;
            record.Child.Entity.Get<AbsoluteTransform>().TransformToRoot = transformToParent * parentTransformToRoot;
        }

        // 递归考察子实体
        foreach (var (_, record) in entity.Get<TreeRelationship<RelativeTransform>.AsParent>().Relationships)
            RecursivelyUpdateAbsoluteTransform(record.Child.Entity);
    }

    [Query]
    [All<TreeRelationship<RelativeTransform>.AsChild, AbsoluteTransform>]
    private static void UpdateFromRoot(Entity root, in TreeRelationship<RelativeTransform>.AsChild asChild)
    {
        // 如果该实体以子身份参与关系，则不是根实体，需要跳过
        if (asChild.Relationship is not null)
            return;

        RecursivelyUpdateAbsoluteTransform(root);
    }
}
