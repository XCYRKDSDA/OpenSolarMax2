// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems.Warping;

[SimulateSystem, BeforeStructuralChanges, Iterate(typeof(WarpingStatus))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class ProgressShipsWarpingSystem(World world) : ITickSystem
{
    [Query]
    [All<WarpingStatus>]
    private static void ProgressEffect(ref WarpingStatus status, [Data] GameTime time)
    {
        if (status.State == WarpingState.PreWarp)
            status.PreWarp.ElapsedTime += time.ElapsedGameTime;
        else if (status.State == WarpingState.PostWarp)
            status.PostWarp.ElapsedTime += time.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => ProgressEffectQuery(world, gameTime);
}

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(WarpingStatus)), Write(typeof(AbsoluteTransform)), Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
// 在自动计算绝对位姿系统之后以覆盖位姿
[ExecuteAfter(typeof(CalculateAbsoluteTransformSystem))]
// 与普通运输系统完全不相干
[FineWith(typeof(CalculateShipPositionSystem)), FineWith(typeof(UpdateJumpingEffectSystem))]
// 动画不会设置颜色，因此和阵营颜色应用系统不相干
[FineWith(typeof(ApplyTeamColorSystem)), FineWith(typeof(SynchronizeColorSystem))]
// 覆盖新生舰船动画
[ExecuteAfter(typeof(ApplyShipPostBornEffectSystem))]
public partial class ApplyShipsWarpingEffectSystem(World world, IAssetsManager assets) : ICalcSystem
{
    private readonly AnimationClip<Entity> _shipPreWarpAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipPreWarp.json");

    private readonly AnimationClip<Entity> _shipPostWarpAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipPostWarp.json");

    [Query]
    [All<WarpingStatus, Sprite, AbsoluteTransform>]
    private void ApplyEffect(Entity ship, in WarpingStatus status, ref AbsoluteTransform pose)
    {
        if (status.State == WarpingState.PreWarp)
        {
            // 面向目标位置
            var head = ship.Get<AbsoluteTransform>().Translation;

            var destinationPlanetPose = status
                .Task.DestinationPlanet.Get<AbsoluteTransform>()
                .TransformToRoot;
            var expectedPoseInDestination = RevolutionUtils
                .CalculateTransform(
                    status.Task.ExpectedRevolutionOrbit,
                    status.Task.ExpectedRevolutionState
                )
                .TransformToParent;
            var tail = (expectedPoseInDestination * destinationPlanetPose).Translation;

            pose.Rotation = TransformProjection.UprightAim(tail - head);

            // 播放动画
            var animationTime = (float)status.PreWarp.ElapsedTime.TotalSeconds;

            if (animationTime < 0.25f) // 用0.5秒渐入
                AnimationEvaluator<Entity>.TweenAndSet(
                    ref ship,
                    null,
                    float.NaN,
                    _shipPreWarpAnimationClip,
                    animationTime,
                    null,
                    animationTime / 0.25f
                );
            else
                AnimationEvaluator<Entity>.EvaluateAndSet(
                    ref ship,
                    _shipPreWarpAnimationClip,
                    animationTime
                );
        }
        else if (status.State == WarpingState.PostWarp)
        {
            // 播放动画
            var animationTime = (float)status.PostWarp.ElapsedTime.TotalSeconds;

            AnimationEvaluator<Entity>.TweenAndSet(
                ref ship,
                _shipPostWarpAnimationClip,
                animationTime,
                null,
                float.NaN,
                null,
                animationTime
            );
        }
    }

    public void Update() => ApplyEffectQuery(world);
}

[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(Sprite)),
    ReadPrev(typeof(TeamReferenceColor)),
    ReadPrev(typeof(TreeRelationship<AbsoluteTransform>.AsChild)),
    ReadPrev(typeof(InTeam.AsAffiliate))
]
[Iterate(typeof(WarpingStatus)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(ProgressShipsWarpingSystem))]
public partial class WarpSystem(World world, IAssetsManager assets, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    private readonly SafeFmodEventDescription _warpingSoundEffect =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/Warping");

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
        ref WarpingStatus status,
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

            status.State = WarpingState.PostWarp;
            status.PostWarp = new() { ElapsedTime = TimeSpan.Zero };

            jobs.Add((departure, destination));
            arrivals.Add((destination, asAffiliate.Relationship!.Value.Copy.Team));
        }
        else if (
            status.State == WarpingState.PostWarp
            && status.PostWarp.ElapsedTime > TimeSpan.FromSeconds(1)
        )
        {
            status.State = WarpingState.Idle;
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

#endif
