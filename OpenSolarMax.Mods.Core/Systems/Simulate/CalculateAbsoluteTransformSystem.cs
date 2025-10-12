using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据相对变换<see cref="RelativeTransform"/>及其树型关系计算每个实体的绝对变换
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>.AsParent)),
 ReadCurr(typeof(TreeRelationship<RelativeTransform>.AsChild))]
[ReadCurr(typeof(RelativeTransform)), Write(typeof(AbsoluteTransform))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CalculateAbsoluteTransformSystem(World world) : ICalcSystem
{
    private static void RecursivelyUpdateAbsoluteTransform(Entity entity)
    {
        var parentTransformToRoot = entity.Get<AbsoluteTransform>().TransformToRoot;

        // 先计算子实体的变换，感觉比递归的cache miss会少一些
        foreach (var (relationship, record) in entity.Get<TreeRelationship<RelativeTransform>.AsParent>().Relationships)
        {
            var transformToParent = relationship.Get<RelativeTransform>().TransformToParent;
            record.Child.Get<AbsoluteTransform>().TransformToRoot = transformToParent * parentTransformToRoot;
        }

        // 递归考察子实体
        foreach (var (_, record) in entity.Get<TreeRelationship<RelativeTransform>.AsParent>().Relationships)
            RecursivelyUpdateAbsoluteTransform(record.Child);
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

    public void Update() => UpdateFromRootQuery(world);
}
