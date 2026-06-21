using System.Diagnostics;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(JumpingStatus)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(AttackRange)),
    Write(typeof(InAttackRangeShipsRegistry))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class GetJumpingUnitsInRangeSystem(World world) : ICalcSystem
{
    [Query]
    [All<InTeam.AsAffiliate, JumpingStatus, AbsoluteTransform>]
    private static void GetJumpingUnitsInRangeForCertainEntity(
        Entity entity,
        in InTeam.AsAffiliate asAffiliate,
        in JumpingStatus jumpingStatus,
        in AbsoluteTransform unitPose,
        [Data] in AbsoluteTransform planetPose,
        [Data] in float range,
        [Data] in Registry<Entity, (Entity Ship, float Distance)> result
    )
    {
        if (jumpingStatus.State == JumpingState.Idle)
            return;

        // 矩形判断，减少计算量
        var diffX = unitPose.Translation.X - planetPose.Translation.X;
        var diffY = unitPose.Translation.Y - planetPose.Translation.Y;
        if (diffX > range || diffX < -range || diffY > range || diffY < -range)
            return;

        // 距离判断
        var distance = MathF.Sqrt(diffX * diffX + diffY * diffY);
        if (distance > range)
            return;

        Debug.Assert(asAffiliate.Relationship is not null);
        result[asAffiliate.Relationship.Value.Copy.Team].Add((entity, distance));
    }

    [Query]
    [All<InAttackRangeShipsRegistry, AttackRange, AbsoluteTransform>]
    private void GetJumpingUnitsInRange(
        ref InAttackRangeShipsRegistry registry,
        in AttackRange attackRange,
        in AbsoluteTransform pose
    )
    {
        foreach (var (_, pairs) in registry.Ships)
            pairs.Clear();
        GetJumpingUnitsInRangeForCertainEntityQuery(
            world,
            in pose,
            in attackRange.Range,
            registry.Ships
        );
        foreach (var (_, pairs) in registry.Ships)
            pairs.Sort((p1, p2) => p1.Item2.CompareTo(p2.Item2));
    }

    public void Update() => GetJumpingUnitsInRangeQuery(world);
}
