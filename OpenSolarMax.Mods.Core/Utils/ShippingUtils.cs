using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game;
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
        var tailProxy = virtualWorld.Construct(in Signatures.Transformable);
        tailProxy.Get<AbsoluteTransform>() = tail.Get<AbsoluteTransform>();

        var child = tail;
        var childProxy = tailProxy;

        while (true)
        {
            // 当发现考察的子实体已经是根实体时，方法结束
            if (!child.TryGet<TreeRelationship<RelativeTransform>.AsChild>(out var asChild))
                return (virtualWorld, tailProxy);
            if (asChild.Relationship is null)
                return (virtualWorld, tailProxy);
            var relationship = asChild.Relationship.Value.Ref;
            var parent = asChild.Relationship.Value.Copy.Parent;

            // 创建虚拟世界中关于原世界父对象的代理对象
            var parentProxy = virtualWorld.Construct(in Signatures.Transformable);
            parentProxy.Get<AbsoluteTransform>() = parent.Get<AbsoluteTransform>();

            // 创建子实体代理和父实体代理之间的关系
            var relationshipRecord =
                new TreeRelationship<RelativeTransform>(parentProxy, childProxy);
            var relationshipProxy =
                relationship.Has<RevolutionOrbit, RevolutionState>()
                    ? virtualWorld.Create(
                        relationshipRecord,
                        relationship.Get<RelativeTransform>(),
                        relationship.Get<RevolutionOrbit>(), relationship.Get<RevolutionState>())
                    : virtualWorld.Create(
                        relationshipRecord,
                        relationship.Get<RelativeTransform>());

            // 将关系直接记录到两侧组件中
            childProxy.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship =
                (relationshipProxy, relationshipRecord);
            parentProxy.Get<TreeRelationship<RelativeTransform>.AsParent>().Relationships.Add(
                relationshipProxy, relationshipRecord);

            child = parent;
            childProxy = parentProxy;
        }
    }

    private static readonly float _dt = 1f;

    public static (Vector3 Destination, float Duration, Vector3 Derivative) CalculateShippingTask(
        Entity departure, Entity destination, Shippable shippable)
    {
        // 获取出发位置
        var departurePosition = departure.Get<AbsoluteTransform>().Translation;

        // 提取最简的变换树
        var (virtualWorld, destinationProxy) = ExtractBareTransforms(destination);
        ref readonly var destinationPositionRef = ref destinationProxy.Get<AbsoluteTransform>().Translation;

        // 生成模拟系统
        var simulateSystems = new Group<GameTime>($"simulateSystem_{virtualWorld.GetHashCode()}",
                                                  new UpdateRevolutionPhaseSystem(virtualWorld),
                                                  new CalculateTransformAroundOrbitSystem(virtualWorld),
                                                  new CalculateAbsoluteTransformSystem(virtualWorld)
        );

        // 开始求解
        var t = 0f;
        var err1 = float.NaN;
        var destinationPosition1 = destinationPositionRef;
        while (true)
        {
            // 步进系统
            t += _dt;
            var simTime = new GameTime(TimeSpan.FromSeconds(t), TimeSpan.FromSeconds(_dt));
            simulateSystems.JustUpdate(in simTime);

            // 计算距离
            var distance = (destinationPositionRef - departurePosition).Length();

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
                           Vector3.Lerp(destinationPosition1, destinationPositionRef, k),
                           MathHelper.Lerp(t - _dt, t, k),
                           (destinationPositionRef - destinationPosition1) / _dt
                       );
            }

            err1 = err;
            destinationPosition1 = destinationPositionRef;
        }
    }
}
