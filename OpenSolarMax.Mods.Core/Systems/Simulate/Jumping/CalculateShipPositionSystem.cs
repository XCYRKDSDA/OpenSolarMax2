using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 跳跃系统。根据跳跃时间计算单位动画、位置和方向
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(JumpingStatus)), Write(typeof(AbsoluteTransform))]
[FineWith(typeof(CalculateAbsoluteTransformSystem))] // 跳跃单位应当不再有相对变换，因此和计算绝对位姿的系统无干扰
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CalculateShipPositionSystem(World world) : ICalcSystem
{
    [Query]
    [All<JumpingStatus, AbsoluteTransform>]
    private static void CalculatePosition(in JumpingStatus status, ref AbsoluteTransform pose)
    {
        if (status.State == JumpingState.Idle)
            return;

        if (status.State == JumpingState.Charging)
            pose.Translation = status.Task.DeparturePosition;
        else if (status.State == JumpingState.Travelling)
        {
            var progress =
                status.Travelling.ElapsedTime
                / (status.Task.ExpectedTravelDuration - status.Travelling.DelayedTime);
            pose.Translation = Vector3.Lerp(
                status.Task.DeparturePosition,
                status.Task.ExpectedArrivalPosition,
                progress
            );
        }

        // 摆放尾向
        // 旋转后的+X轴指向目标点, XZ平面与原XY平面垂直
        var headX = Vector3.Normalize(
            status.Task.ExpectedArrivalPosition - status.Task.DeparturePosition
        );
        var headY = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, headX));
        var headZ = Vector3.Normalize(Vector3.Cross(headX, headY));
        var rotation = new Matrix
        {
            Right = headX,
            Up = headY,
            Backward = headZ,
        };
        pose.Rotation = Quaternion.CreateFromRotationMatrix(rotation);
    }

    public void Update() => CalculatePositionQuery(world);
}
