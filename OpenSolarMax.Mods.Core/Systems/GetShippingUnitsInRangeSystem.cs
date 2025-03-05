using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(CalculateShipPositionSystem))]
[ExecuteAfter(typeof(CalculateAbsoluteTransformSystem))]
[ExecuteAfter(typeof(IndexPartyAffiliationSystem))]
public sealed partial class GetShippingUnitsInRangeSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private static bool InRange(Entity ship, in AbsoluteTransform planetPose, in float range)
    {
        if (ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship is not null)
            return false;

        ref readonly var unitPose = ref ship.Get<AbsoluteTransform>();

        // 矩形判断，减少计算量
        var diffX = unitPose.Translation.X - planetPose.Translation.X;
        var diffY = unitPose.Translation.Y - planetPose.Translation.Y;
        if (diffX > range || diffX < -range || diffY > range || diffY < -range)
            return false;

        // 距离判断
        var distance = MathF.Sqrt(diffX * diffX + diffY * diffY);
        return distance <= range;
    }

    private static readonly QueryDescription _shipDesc =
        new QueryDescription().WithAll<InParty.AsAffiliate, ShippingStatus, AbsoluteTransform>();

    [Query]
    [All<InAttackRangeShipsRegistry, AttackRange, AbsoluteTransform>]
    private static void GetShippingUnitsInRange(ref InAttackRangeShipsRegistry registry,
                                                AttackRange attackRange, AbsoluteTransform pose,
                                                [Data] in Entity[] ships)
    {
        registry.Ships = ships.Where(e => InRange(e, in pose, in attackRange.Range))
                              .ToLookup(e => e.Get<InParty.AsAffiliate>().Relationship!.Value.Copy.Party,
                                        e => e.Reference());
    }

    public override void Update(in GameTime t)
    {
        var ships = new Entity[World.CountEntities(in _shipDesc)];
        World.GetEntities(in _shipDesc, ships.AsSpan());
        GetShippingUnitsInRangeQuery(World, ships);
    }
}
