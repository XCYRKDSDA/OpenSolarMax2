using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

public delegate bool? CheckLocationReachabilityCallback(
    World world,
    Entity departure,
    in AbsoluteTransform departurePose,
    in Vector3 destinationPose
);

/// <summary>
/// 计算每条拖拽线段的出发星球能否到达目标位置。
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(MouseDeviceState))]
[ReadCurr(typeof(PlanetManeuveringLineSource.AsLine))]
[ReadCurr(typeof(PlanetManeuveringLineTarget.AsLine))]
[Calc(typeof(ManeuveringLineReachability))]
public sealed partial class CalculateManeuveringLineReachabilitySystem(World world) : ICalcSystem
{
    [Hook("CheckLocationReachability")]
    public CheckLocationReachabilityCallback? CheckReachabilityDelegate { get; set; }

    private bool CheckReachability(Entity departure, Vector3 destination)
    {
        foreach (var @delegate in CheckReachabilityDelegate?.GetInvocationList() ?? [])
        {
            var checker = (CheckLocationReachabilityCallback)@delegate;
            var result = checker.Invoke(
                world,
                departure,
                in departure.Get<AbsoluteTransform>(),
                in destination
            );
            if (result is not null)
                return result.Value;
        }

        return !ManeuveringUtils.CheckBarriersBlocking(
            world,
            departure.Get<AbsoluteTransform>().Translation,
            destination
        );
    }

    [Query]
    [All<
        PlanetManeuveringLineSource.AsLine,
        PlanetManeuveringLineTarget.AsLine,
        ManeuveringLineReachability
    >]
    private void UpdateReachability(Entity entity, ref ManeuveringLineReachability reachability)
    {
        // 俩组件类型都叫 AsLine，Arch.System.SourceGenerator 生成时会冲突，因此只能专门取
        ref readonly var asSource = ref entity.Get<PlanetManeuveringLineSource.AsLine>();
        ref readonly var asTarget = ref entity.Get<PlanetManeuveringLineTarget.AsLine>();

        var source = asSource.Relationship!.Value.Copy;

        // 计算目标位置
        Vector3 targetPosition;
        if (asTarget.Relationship is { } targetRelationship)
        {
            targetPosition = targetRelationship.Copy.Planet.Get<AbsoluteTransform>().Translation;
        }
        else
        {
            ref readonly var projection = ref source.View.Get<Projection>();
            ref readonly var mouse = ref source.View.Get<MouseDeviceState>();
            targetPosition = Vector3.Transform(
                new Vector3(mouse.State.Position.ToVector2(), 0),
                Matrix.Invert(projection.WorldToScreen)
            );
        }

        reachability.Reachable = CheckReachability(source.Planet, targetPosition);
    }

    public void Update() => UpdateReachabilityQuery(world);
}
