using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnchorageUtils
{
    private const float _defaultRevolutionOffsetRange = 0.3f;

    public static void AnchorTo(this Entity ship, Entity planet, Random? random = null,
                                        float revolutionOffsetRange = _defaultRevolutionOffsetRange)
    {
        random ??= new();

        // 设置停靠关系
        ship.SetParent<Components.Anchorage>(planet);

        // 设置变换关系
        ship.SetParent<RelativeTransform>(planet);

        // 随机生成并泊入轨道
        ship.Get<RevolutionOrbit>() = RevolutionUtils.CreateRandomRevolutionOrbit(
            in planet.Get<PlanetGeostationaryOrbit>(), random, revolutionOffsetRange);
        ship.Get<RevolutionState>() = RevolutionUtils.CreateRandomState(random);
    }

    //public static void VisuallyAnchorTo(this Entity ship, Entity planet, in Matrix view)
    public static void JustAnchorTo(this Entity ship, Entity planet)
    {
        // 设置停靠关系
        ship.SetParent<Anchorage>(planet);

        // 设置变换关系
        ship.SetParent<RelativeTransform>(planet);

        //// 根据当前位置计算视觉上合适的公转轨道

        //// 计算星球轨道平面
        //ref readonly var planetPose = ref planet.Get<AbsoluteTransform>();
        //ref readonly var planetOrbit = ref planet.Get<PlanetGeostationaryOrbit>();
        //var planetOrbitPlane = (
        //    Position: planetPose.Translation,
        //    Normal: Vector3.TransformNormal(Vector3.UnitZ, Matrix.CreateFromQuaternion(planetOrbit.Rotation * planetPose.Rotation))
        //);

        //// 计算单位位置
        //var shipPosition = ship.Get<AbsoluteTransform>().Translation;

        //// 将单位位置反向投射到星球的轨道平面
        //var shipPositionProjection = (
        //    Position: shipPosition,
        //    Direction: Vector3.TransformNormal(Vector3.UnitZ, Matrix.Invert(view))
        //);
        //var projectedShipPosition =
        //    shipPositionProjection.Position
        //    + shipPositionProjection.Direction
        //      * (Vector3.Dot(planetOrbitPlane.Position - shipPositionProjection.Position, planetOrbitPlane.Normal)
        //         / Vector3.Dot(shipPositionProjection.Direction, planetOrbitPlane.Normal));

        //// 设置位置，并计算单位的轨道
        //ship.Get<AbsoluteTransform>().Translation = projectedShipPosition;
        //var radius = (projectedShipPosition - planetPose.Translation).Length();
    }

    public static void UnanchorTo(this Entity ship, Entity planet)
    {
        // 解除停靠关系
        ship.RemoveParent<Anchorage>();

        // 解除变换树关系，但是保持当前绝对位姿
        ship.RemoveParent<RelativeTransform>();
        ship.Get<RelativeTransform>().TransformToParent = ship.Get<AbsoluteTransform>().TransformToRoot;

        // 移除轨道
        ship.Remove<RevolutionOrbit, RevolutionState>();
    }
}
