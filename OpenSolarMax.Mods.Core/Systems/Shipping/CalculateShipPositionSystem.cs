using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 运输系统。根据运输时间计算单位动画、位置和方向
/// </summary>
[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteBefore(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class CalculateShipPositionSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingStatus, AbsoluteTransform>]
    private static void CalculatePosition(in ShippingStatus status, ref AbsoluteTransform pose)
    {
        if (status.State == ShippingState.Idle) return;

        if (status.State == ShippingState.Charging)
            pose.Translation = status.Task.DeparturePosition;
        else if (status.State == ShippingState.Travelling)
        {
            var progress = status.Travelling.ElapsedTime /
                           (status.Task.ExpectedTravelDuration - status.Travelling.DelayedTime);
            pose.Translation = Vector3.Lerp(status.Task.DeparturePosition, status.Task.ExpectedArrivalPosition,
                                            progress);
        }

        // 摆放尾向
        // 旋转后的+X轴指向目标点, XZ平面与原XY平面垂直
        var headX = Vector3.Normalize(status.Task.ExpectedArrivalPosition - status.Task.DeparturePosition);
        var headY = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, headX));
        var headZ = Vector3.Normalize(Vector3.Cross(headX, headY));
        var rotation = new Matrix { Right = headX, Up = headY, Backward = headZ };
        pose.Rotation = Quaternion.CreateFromRotationMatrix(rotation);
    }
}
