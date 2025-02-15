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
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems.Transportation;

[CoreUpdateSystem]
public partial class ProgressUnitsTransportationSystem(World world, IAssetsManager _)
    : BaseSystem<World, GameTime>(world), ISystem
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
}

[LateUpdateSystem]
public partial class ApplyUnitsTransportationEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
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

            var destinationPlanetPose = status.Task.DestinationPlanet.Entity.Get<AbsoluteTransform>().TransformToRoot;
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

            if (animationTime < 0.5f) // 用0.5秒渐入
                AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                       null, float.NaN,
                                                       _unitPreTransportationAnimationClip, animationTime,
                                                       null, animationTime / 0.5f);
            else
                AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitPreTransportationAnimationClip, animationTime);
        }
        else if (status.State == TransportingState.PostTransportation)
        {
            // 播放动画
            var animationTime = (float)status.PostTransportation.ElapsedTime.TotalSeconds;

            if (animationTime < 0.25f) // 用0.5秒渐出
                AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitPostTransportationAnimationClip,
                                                          animationTime);
            else
                AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                       _unitPostTransportationAnimationClip, animationTime,
                                                       null, float.NaN,
                                                       null, (animationTime - 0.25f) / 0.5f);
        }
    }
}

[StructuralChangeSystem]
public partial class TransportUnitsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<TransportingStatus, TreeRelationship<Anchorage>.AsChild, TreeRelationship<RelativeTransform>.AsChild, InParty.AsAffiliate>]
    private void TransportUnits(Entity ship, ref TransportingStatus status,
                                in TreeRelationship<Anchorage>.AsChild asChild, in InParty.AsAffiliate asAffiliate)
    {
        if (status.State == TransportingState.PreTransportation &&
            status.PreTransportation.ElapsedTime > TimeSpan.FromSeconds(0.75))
        {
            var departure = asChild.Relationship!.Value.Copy.Parent;
            var destination = status.Task.DestinationPlanet;

            AnchorageUtils.UnanchorShipFromPlanet(ship, asChild.Relationship!.Value.Copy.Parent);
            var (_, newTfRelationship) = AnchorageUtils.AnchorShipToPlanet(ship, status.Task.DestinationPlanet);
            newTfRelationship.Set(status.Task.ExpectedRevolutionOrbit, status.Task.ExpectedRevolutionState);

            World.Make(new TransportationTrailTemplate(assets)
            {
                Head = ship.Get<AbsoluteTransform>().Translation,
                Tail = (RevolutionUtils
                        .CalculateTransform(status.Task.ExpectedRevolutionOrbit, status.Task.ExpectedRevolutionState)
                        .TransformToParent *
                        destination.Entity.Get<AbsoluteTransform>().TransformToRoot).Translation,
                Color = asAffiliate.Relationship!.Value.Copy.Party.Entity.Get<PartyReferenceColor>().Value
            });

            status.State = TransportingState.PostTransportation;
            status.PostTransportation = new() { ElapsedTime = TimeSpan.Zero };
        }
        else if (status.State == TransportingState.PostTransportation &&
                 status.PostTransportation.ElapsedTime > TimeSpan.FromSeconds(0.75))
        {
            status.State = TransportingState.Idle;
        }
    }
}
