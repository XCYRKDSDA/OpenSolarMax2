using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems;

namespace OpenSolarMax.Mods.Core.Utils;

public static class ShippingUtils
{
    //private enum TransformLinkType
    //{
    //    Fixed,
    //    Revolution
    //}

    //private struct TransformLink_Fixed
    //{
    //    public Matrix TransformToParent;
    //}

    //private struct TransformLink_Revolution
    //{
    //    public RevolutionOrbit Orbit;

    //    public RevolutionState InitState;
    //}

    //[StructLayout(LayoutKind.Explicit)]
    //private struct TransformLink
    //{
    //    [FieldOffset(0)]
    //    public TransformLinkType Type;

    //    [FieldOffset(8)]
    //    public TransformLink_Fixed Fixed;

    //    [FieldOffset(8)]
    //    public TransformLink_Revolution Revolution;
    //}

    //private static Matrix CalculatePose(TransformLink[] branch, float time)
    //{
    //    var pose = Matrix.Identity;
    //    for (int i = 0; i < branch.Length; i++)
    //    {
    //        ref readonly var link = ref branch[i];

    //        pose *= link.Type switch
    //        {
    //            TransformLinkType.Fixed => link.Fixed.TransformToParent,
    //            TransformLinkType.Revolution => RevolutionUtils.CalculateTransform(in link.Revolution.Orbit,
    //                new RevolutionState() { Phase = link.Revolution.InitState.Phase + time / link.Revolution.Orbit.Period * 2 * MathF.PI }).TransformToParent,
    //            _ => throw new ArgumentOutOfRangeException(),
    //        };
    //    }
    //    return pose;
    //}

    //public static TransformLink[] GenerateTransformBranch(Entity entity)
    //{
    //    var branch = new List<TransformLink>();
    //    while (true)
    //    {
    //        if (!entity.Has<Tree<RelativeTransform>.Child>()             // 如果不作为相对变换的子节点
    //            || entity.GetParent<RelativeTransform>() == Entity.Null) // 或者相对变换的父节点不存在
    //        {
    //            branch.Add(new()
    //            {
    //                Type = TransformLinkType.Fixed,
    //                Fixed = new() { TransformToParent = entity.Get<AbsoluteTransform>().TransformToRoot }
    //            });
    //            break;
    //        }

    //        if (entity.Has<RevolutionOrbit, RevolutionState>()) // 如果实体按照轨道运行在其父实体周围
    //        {
    //            branch.Add(new()
    //            {
    //                Type = TransformLinkType.Revolution,
    //                Revolution = new()
    //                {
    //                    Orbit = entity.Get<RevolutionOrbit>(),
    //                    InitState = entity.Get<RevolutionState>()
    //                }
    //            });
    //        }
    //    }
    //}

    private static (World, Entity) ExtractBareTransforms(Entity tail)
    {
        var virtualWorld = World.Create();
        var tailProxy = virtualWorld.Construct(in Archetypes.Transformable);
        tailProxy.Get<AbsoluteTransform>() = tail.Get<AbsoluteTransform>();
        tailProxy.Get<RelativeTransform>() = tail.Get<RelativeTransform>();

        var child = tail;
        var childProxy = tailProxy;
        var parent = child.TryGet<Tree<RelativeTransform>.Child>(out var asChild) ? asChild.Parent : Entity.Null;
        while (parent != Entity.Null)
        {
            var parentProxy = virtualWorld.Construct(in Archetypes.Transformable);
            parentProxy.Get<AbsoluteTransform>() = parent.Get<AbsoluteTransform>();
            parentProxy.Get<RelativeTransform>() = parent.Get<RelativeTransform>();

            childProxy.SetParent<RelativeTransform>(parentProxy);

            if (child.Has<RevolutionOrbit, RevolutionState>())
            {
                childProxy.Add<RevolutionOrbit, RevolutionState>();
                childProxy.Get<RevolutionOrbit>() = child.Get<RevolutionOrbit>();
                childProxy.Get<RevolutionState>() = child.Get<RevolutionState>();
            }

            child = parent;
            childProxy = parentProxy;
            parent = child.TryGet<Tree<RelativeTransform>.Child>(out var asChild2) ? asChild2.Parent : Entity.Null;
        }

        return (virtualWorld, tailProxy);
    }

    private static readonly float _dt = 1f;

    public static (Vector3 Destination, float Duration) CalculateShippingTask(Entity departure, Entity destination, Shippable shippable)
    {
        // 获取出发位置
        var departurePosition = departure.Get<AbsoluteTransform>().Translation;

        // 提取最简的变换树
        var (virtualWorld, destinationProxy) = ExtractBareTransforms(destination);
        ref readonly var destinationPosition = ref destinationProxy.Get<AbsoluteTransform>().Translation;

        // 生成模拟系统
        var simulateSystems = new Group<GameTime>($"simulateSystem_{virtualWorld.GetHashCode()}",
            new UpdateRevolutionPhaseSystem(virtualWorld, null),
            new CalculateEntitiesTransformAroundOrbitSystem(virtualWorld, null),
            new CalculateAbsoluteTransformSystem(virtualWorld, null)
        );

        // 开始求解
        var t = 0f;
        var err1 = float.NaN;
        var destinationPosition1 = destinationPosition;
        while (true)
        {
            // 步进系统
            t += _dt;
            var simTime = new GameTime(TimeSpan.FromSeconds(t), TimeSpan.FromSeconds(_dt));
            simulateSystems.BeforeUpdate(in simTime);
            simulateSystems.Update(in simTime);
            simulateSystems.AfterUpdate(in simTime);

            // 计算距离
            var distance = (destinationPosition - departurePosition).Length();

            // 单位可移动的距离
            var movedDistance = shippable.Speed * t;

            // 计算误差
            var err = distance - movedDistance;

            // 当移动的距离首次大于实际距离时，存在一次解
            if (err <= 0)
            {
                // 上一刻为t，误差为err；此刻为t2，误差为err2。做一个线性近似
                var k = (0 - err1) / (err - err1);
                return (
                    Vector3.Lerp(destinationPosition1, destinationPosition, k),
                    MathHelper.Lerp(t - _dt, t, k)
                );
            }

            err1 = err;
            destinationPosition1 = destinationPosition;
        }
    }

    public static void Ship(Entity departure, Entity destination, Entity camp, int expectedNum)
    {
        Debug.Assert(departure.WorldId == destination.WorldId && departure.WorldId == camp.WorldId);

        var shipsRemain = expectedNum;
        var allShips = departure.Get<AnchoredShipsRegistry>().Ships[camp];

        var shippable = camp.Get<Shippable>();
        var (expectedArrivalPlanetPosition, expectedTravelDuration) = CalculateShippingTask(departure, destination, shippable);

        var commonShippingTask = new ShippingTask()
        {
            DestinationPlanet = destination,
            ExpectedTravelDuration = expectedTravelDuration
        };

        var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;

            // 添加运输任务
            ship.Add<ShippingTask, ShippingState>();
            ref var shippingTask = ref ship.Get<ShippingTask>();
            ref var shippingState = ref ship.Get<ShippingState>();

            // 获取相关信息
            ref readonly var pose = ref ship.Get<AbsoluteTransform>();
            ref readonly var revolutionOrbit = ref ship.Get<RevolutionOrbit>();
            ref readonly var revolutionState = ref ship.Get<RevolutionState>();
            ref readonly var departurePlanetOrbit = ref departure.Get<PlanetGeostationaryOrbit>();
            ref readonly var destinationPlanetOrbit = ref destination.Get<PlanetGeostationaryOrbit>();

            // 计算泊入轨道
            var orbitOffset = revolutionOrbit.Shape.Width / 2 / departurePlanetOrbit.Radius;
            var expectedOrbit = new RevolutionOrbit()
            {
                Rotation = destinationPlanetOrbit.Rotation,
                Shape = new(destinationPlanetOrbit.Radius * orbitOffset * 2, destinationPlanetOrbit.Radius * orbitOffset * 2),
                Period = destinationPlanetOrbit.Period * MathF.Pow(orbitOffset, 1.5f)
            };
            var expectedPosition = expectedArrivalPlanetPosition
                                   + RevolutionUtils.CalculateTransform(in expectedOrbit, in revolutionState).Translation;

            // 设置任务
            shippingTask = commonShippingTask with
            {
                DeparturePosition = pose.Translation,
                ExpectedArrivalPosition = expectedPosition,
                ExpectedRevolutionOrbit = expectedOrbit,
                ExpectedRevolutionState = revolutionState
            };
            shippingState.TravelledTime = 0;

            // 解除到星球的锚定
            ship.UnanchorTo(departure);
        }
    }
}
