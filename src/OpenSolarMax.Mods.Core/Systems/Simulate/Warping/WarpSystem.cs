using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(Sprite))]
[ReadCurr(typeof(TeamReferenceColor))]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>.AsChild))]
[ReadCurr(typeof(InTeam.AsAffiliate))]
[ReadCurr(typeof(WarpingStatus))]
[DelayedCalc]
public sealed partial class WarpSystem(World world, IAssetsManager assets, IConceptFactory factory)
    : IDelayedCalcSystem
{
    private readonly SafeFmodEventDescription _warpingSoundEffect =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/Warping");

    [Query]
    [All<
        WarpingStatus,
        AbsoluteTransform,
        Sprite,
        TreeRelationship<RelativeTransform>.AsChild,
        InTeam.AsAffiliate
    >]
    private void Warp(
        Entity ship,
        in WarpingStatus status,
        in AbsoluteTransform pose,
        in Sprite sprite,
        in TreeRelationship<RelativeTransform>.AsChild asChild,
        in InTeam.AsAffiliate asAffiliate,
        [Data] HashSet<(Entity, Entity)> jobs,
        [Data] HashSet<(Entity, Entity)> arrivals,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (
            status.State == WarpingState.PreWarp
            && status.PreWarp.ElapsedTime > TimeSpan.FromSeconds(0.9333)
        )
        {
            var departure = asChild.Relationship!.Value.Copy.Parent;
            var destination = status.Task.DestinationPlanet;

            factory.Make(
                world,
                commandBuffer,
                new ShipAfterImageDescription()
                {
                    Position = pose.Translation,
                    Rotation = pose.Rotation,
                    Color = sprite.Color,
                }
            );

            // 解除到出发星球的公转关系（Anchorage 已在 StartWarpingSystem 中销毁）
            commandBuffer.Destroy(asChild.Relationship!.Value.Ref);
            // 锚定舰船到新星球
            factory.Make(
                world,
                commandBuffer,
                new AnchorageDescription() { Planet = status.Task.DestinationPlanet, Ship = ship }
            );
            factory.Make(
                world,
                commandBuffer,
                new RevolutionDescription()
                {
                    Parent = status.Task.DestinationPlanet,
                    Child = ship,
                    Shape = status.Task.ExpectedRevolutionOrbit.Shape,
                    Period = status.Task.ExpectedRevolutionOrbit.Period,
                    Rotation = status.Task.ExpectedRevolutionOrbit.Rotation,
                    InitPhase = status.Task.ExpectedRevolutionState.Phase,
                }
            );

            factory.Make(
                world,
                commandBuffer,
                new WarpTrailDescription()
                {
                    Head = ship.Get<AbsoluteTransform>().Translation,
                    Tail = (
                        RevolutionUtils
                            .CalculateTransform(
                                status.Task.ExpectedRevolutionOrbit,
                                status.Task.ExpectedRevolutionState
                            )
                            .TransformToParent
                        * destination.Get<AbsoluteTransform>().TransformToRoot
                    ).Translation,
                    Color = asAffiliate
                        .Relationship!.Value.Copy.Team.Get<TeamReferenceColor>()
                        .Value,
                }
            );

            commandBuffer.Set(
                ship,
                new WarpingStatus()
                {
                    State = WarpingState.PostWarp,
                    PostWarp = new() { ElapsedTime = TimeSpan.Zero },
                }
            );

            jobs.Add((departure, destination));
            arrivals.Add((destination, asAffiliate.Relationship!.Value.Copy.Team));
        }
        else if (
            status.State == WarpingState.PostWarp
            && status.PostWarp.ElapsedTime > TimeSpan.FromSeconds(1)
        )
        {
            commandBuffer.Set(ship, new WarpingStatus() { State = WarpingState.Idle });
        }
    }

    private readonly HashSet<(Entity, Entity)> _jobs = [];
    private readonly HashSet<(Entity, Entity)> _arrivalsPerFrame = [];

    public void Update(CommandBuffer commandBuffer)
    {
        _jobs.Clear();
        _arrivalsPerFrame.Clear();
        WarpQuery(world, _jobs, _arrivalsPerFrame, commandBuffer);

        // 对每个阵营每次抵达只创建一个抵达效果
        foreach (var (destination, team) in _arrivalsPerFrame)
        {
            factory.Make(
                world,
                commandBuffer,
                new DestinationEffectDescription()
                {
                    Warp = destination,
                    Color = team.Get<TeamReferenceColor>().Value,
                    WarpRadius = destination.Get<ReferenceSize>().Radius,
                }
            );
        }

        // 对每组从某个起点到某个终点的传送任务只创建一个传送音效
        foreach (var (departure, destination) in _jobs)
        {
            // 计算音效位置
            var center =
                (
                    departure.Get<AbsoluteTransform>().Translation
                    + destination.Get<AbsoluteTransform>().Translation
                ) / 2;

            // 创建音效
            factory.Make(
                world,
                commandBuffer,
                new SimpleSoundDescription()
                {
                    Transform = new AbsoluteTransformOptions()
                    {
                        Translation = center,
                        Rotation = Quaternion.Identity,
                    },
                    SoundEffect = _warpingSoundEffect,
                }
            );
        }
    }
}
