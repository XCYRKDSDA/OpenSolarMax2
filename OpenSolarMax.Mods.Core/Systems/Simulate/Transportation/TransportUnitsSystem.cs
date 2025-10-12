using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using OpenSolarMax.Mods.Core.Templates.Options;
using OpenSolarMax.Mods.Core.Utils;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Systems.Transportation;

[SimulateSystem, BeforeStructuralChanges, Iterate(typeof(TransportingStatus))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class ProgressUnitsTransportationSystem(World world) : ITickSystem
{
    [Query]
    [All<TransportingStatus>]
    private static void ProgressEffect(ref TransportingStatus status, [Data] GameTime time)
    {
        if (status.State == TransportingState.PreTransportation)
            status.PreTransportation.ElapsedTime += time.ElapsedGameTime;
        else if (status.State == TransportingState.PostTransportation)
            status.PostTransportation.ElapsedTime += time.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => ProgressEffectQuery(world, gameTime);
}

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TransportingStatus))]
[Write(typeof(AbsoluteTransform)), Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
// 在自动计算绝对位姿系统之后以覆盖位姿
[ExecuteAfter(typeof(CalculateAbsoluteTransformSystem))]
// 与普通运输系统完全不相干
[FineWith(typeof(CalculateShipPositionSystem)), FineWith(typeof(UpdateShippingEffectSystem))]
// 动画不会设置颜色，因此和阵营颜色应用系统不相干
[FineWith(typeof(ApplyPartyColorSystem))]
// 覆盖新生单位动画
[ExecuteAfter(typeof(ApplyUnitPostBornEffectSystem))]
public partial class ApplyUnitsTransportationEffectSystem(World world, IAssetsManager assets) : ICalcSystem
{
    private readonly AnimationClip<Entity> _unitPreTransportationAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitPreTransportation.json");

    private readonly AnimationClip<Entity> _unitPostTransportationAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitPostTransportation.json");

    [Query]
    [All<TransportingStatus, Sprite, AbsoluteTransform>]
    private void ApplyEffect(Entity ship, in TransportingStatus status, ref AbsoluteTransform pose)
    {
        if (status.State == TransportingState.PreTransportation)
        {
            // 面向目标位置
            var head = ship.Get<AbsoluteTransform>().Translation;

            var destinationPlanetPose = status.Task.DestinationPlanet.Get<AbsoluteTransform>().TransformToRoot;
            var expectedPoseInDestination = RevolutionUtils.CalculateTransform(status.Task.ExpectedRevolutionOrbit,
                                                                               status.Task.ExpectedRevolutionState)
                                                           .TransformToParent;
            var tail = (expectedPoseInDestination * destinationPlanetPose).Translation;

            var vector = tail - head;
            var unitX = Vector3.Normalize(vector);
            var unitY = Vector3.Normalize(new(-vector.Y, vector.X, 0));
            var unitZ = Vector3.Cross(unitX, unitY);
            var rotation = new Matrix { Right = unitX, Up = unitY, Backward = unitZ };
            pose.Rotation = Quaternion.CreateFromRotationMatrix(rotation);

            // 播放动画
            var animationTime = (float)status.PreTransportation.ElapsedTime.TotalSeconds;

            if (animationTime < 0.25f) // 用0.5秒渐入
                AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                       null, float.NaN,
                                                       _unitPreTransportationAnimationClip, animationTime,
                                                       null, animationTime / 0.25f);
            else
                AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitPreTransportationAnimationClip, animationTime);
        }
        else if (status.State == TransportingState.PostTransportation)
        {
            // 播放动画
            var animationTime = (float)status.PostTransportation.ElapsedTime.TotalSeconds;

            AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                   _unitPostTransportationAnimationClip, animationTime,
                                                   null, float.NaN,
                                                   null, animationTime);
        }
    }

    public void Update() => ApplyEffectQuery(world);
}

[SimulateSystem, BeforeStructuralChanges]
[ReadPrev(typeof(AbsoluteTransform)), ReadPrev(typeof(Sprite)), ReadPrev(typeof(PartyReferenceColor)),
 ReadPrev(typeof(TreeRelationship<Anchorage>.AsChild)), ReadPrev(typeof(TreeRelationship<AbsoluteTransform>.AsChild)),
 ReadPrev(typeof(InParty.AsAffiliate))]
[Iterate(typeof(TransportingStatus)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(ProgressUnitsTransportationSystem))]
public partial class TransportUnitsSystem(World world, IAssetsManager assets) : ICalcSystemWithStructuralChanges
{
    private FmodEventDescription _warpingSoundEffect = assets.Load<FmodEventDescription>("Sounds/Master.bank:/Warping");

    [Query]
    [All<TransportingStatus, AbsoluteTransform, Sprite, TreeRelationship<Anchorage>.AsChild,
        TreeRelationship<RelativeTransform>.AsChild, InParty.AsAffiliate>]
    private void TransportUnits(Entity ship, ref TransportingStatus status,
                                in AbsoluteTransform pose, in Sprite sprite,
                                in TreeRelationship<Anchorage>.AsChild asChild, in InParty.AsAffiliate asAffiliate,
                                [Data] HashSet<(Entity, Entity)> jobs,
                                [Data] HashSet<(Entity, Entity)> arrivals,
                                [Data] CommandBuffer commandBuffer)
    {
        if (status.State == TransportingState.PreTransportation &&
            status.PreTransportation.ElapsedTime > TimeSpan.FromSeconds(0.9333))
        {
            var departure = asChild.Relationship!.Value.Copy.Parent;
            var destination = status.Task.DestinationPlanet;

            world.Make(commandBuffer, new UnitAfterImageTemplate(assets)
            {
                Position = pose.Translation,
                Rotation = pose.Rotation,
                Color = sprite.Color
            });

            // 解除到星球的锚定
            commandBuffer.Destroy(ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship!.Value.Ref);
            commandBuffer.Destroy(ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref);
            // 锚定单位到新星球
            world.Make(commandBuffer, new AnchorageTemplate()
            {
                Planet = status.Task.DestinationPlanet,
                Ship = ship
            });
            world.Make(commandBuffer, new RevolutionTemplate()
            {
                Parent = status.Task.DestinationPlanet,
                Child = ship,
                Shape = status.Task.ExpectedRevolutionOrbit.Shape,
                Period = status.Task.ExpectedRevolutionOrbit.Period,
                Rotation = status.Task.ExpectedRevolutionOrbit.Rotation,
                InitPhase = status.Task.ExpectedRevolutionState.Phase
            });

            world.Make(commandBuffer, new TransportationTrailTemplate(assets)
            {
                Head = ship.Get<AbsoluteTransform>().Translation,
                Tail = (RevolutionUtils
                        .CalculateTransform(status.Task.ExpectedRevolutionOrbit, status.Task.ExpectedRevolutionState)
                        .TransformToParent *
                        destination.Get<AbsoluteTransform>().TransformToRoot).Translation,
                Color = asAffiliate.Relationship!.Value.Copy.Party.Get<PartyReferenceColor>().Value
            });

            status.State = TransportingState.PostTransportation;
            status.PostTransportation = new() { ElapsedTime = TimeSpan.Zero };

            jobs.Add((departure, destination));
            arrivals.Add((destination, asAffiliate.Relationship!.Value.Copy.Party));
        }
        else if (status.State == TransportingState.PostTransportation &&
                 status.PostTransportation.ElapsedTime > TimeSpan.FromSeconds(1))
        {
            status.State = TransportingState.Idle;
        }
    }

    private readonly HashSet<(Entity, Entity)> _jobs = [];
    private readonly HashSet<(Entity, Entity)> _arrivalsPerFrame = [];

    public void Update(CommandBuffer commandBuffer)
    {
        _jobs.Clear();
        _arrivalsPerFrame.Clear();
        TransportUnitsQuery(world, _jobs, _arrivalsPerFrame, commandBuffer);

        // 对每个阵营每次抵达只创建一个抵达效果
        foreach (var (destination, party) in _arrivalsPerFrame)
        {
            world.Make(commandBuffer, new DestinationEffectTemplate(assets)
            {
                Portal = destination,
                Color = party.Get<PartyReferenceColor>().Value,
                PortalRadius = destination.Get<ReferenceSize>().Radius
            });
        }

        // 对每组从某个起点到某个终点的传送任务只创建一个传送音效
        foreach (var (departure, destination) in _jobs)
        {
            // 计算音效位置
            var center = (departure.Get<AbsoluteTransform>().Translation +
                          destination.Get<AbsoluteTransform>().Translation) / 2;

            // 创建音效
            world.Make(commandBuffer, new SimpleSoundTemplate()
            {
                Transform = new AbsoluteTransformOptions() { Translation = center, Rotation = Quaternion.Identity },
                SoundEffect = _warpingSoundEffect,
            });
        }
    }
}
