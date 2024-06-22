using System.Diagnostics;
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
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 处理<see cref="StartShippingRequest"/>来使单位开始飞行的系统
/// </summary>
[StructuralChangeSystem]
public sealed partial class StartShippingSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();
    private readonly UnitTrailTemplate _trailTemplate = new(assets);

    [Query]
    [All<StartShippingRequest>]
    private void StartShipping(Entity requestEntity, in StartShippingRequest request)
    {
        Debug.Assert(requestEntity.WorldId == request.Departure.Entity.WorldId
                     && requestEntity.WorldId == request.Destination.Entity.WorldId
                     && requestEntity.WorldId == request.Party.Entity.WorldId);

        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Entity.Get<AnchoredShipsRegistry>().Ships[request.Party];

        var shippable = request.Party.Entity.Get<Shippable>();
        var (expectedArrivalPlanetPosition, expectedTravelDuration) =
            ShippingUtils.CalculateShippingTask(request.Departure, request.Destination, shippable);

        var commonShippingTask = new ShippingTask()
        {
            DestinationPlanet = request.Destination,
            ExpectedTravelDuration = expectedTravelDuration
        };

        var world = World.Worlds[requestEntity.WorldId];
        var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;

            // 添加运输任务
            ship.Entity.Add<ShippingTask, ShippingState>();
            ref var shippingTask = ref ship.Entity.Get<ShippingTask>();
            ref var shippingState = ref ship.Entity.Get<ShippingState>();

            // 获取相关信息
            ref readonly var pose = ref ship.Entity.Get<AbsoluteTransform>();
            var transformRelationship =
                ship.Entity.Get<TreeRelationship<RelativeTransform>.AsChild>().Index.Relationship;
            ref readonly var revolutionOrbit = ref transformRelationship.Entity.Get<RevolutionOrbit>();
            ref readonly var revolutionState = ref transformRelationship.Entity.Get<RevolutionState>();
            ref readonly var departurePlanetOrbit = ref request.Departure.Entity.Get<PlanetGeostationaryOrbit>();
            ref readonly var destinationPlanetOrbit = ref request.Destination.Entity.Get<PlanetGeostationaryOrbit>();

            // 计算泊入轨道
            var orbitOffset = revolutionOrbit.Shape.Width / 2 / departurePlanetOrbit.Radius;
            var expectedOrbit = new RevolutionOrbit()
            {
                Rotation = destinationPlanetOrbit.Rotation,
                Shape = new(destinationPlanetOrbit.Radius * orbitOffset * 2,
                            destinationPlanetOrbit.Radius * orbitOffset * 2),
                Period = destinationPlanetOrbit.Period * MathF.Pow(orbitOffset, 1.5f)
            };
            var expectedPosition = expectedArrivalPlanetPosition
                                   + RevolutionUtils.CalculateTransform(in expectedOrbit, in revolutionState)
                                                    .Translation;

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
            AnchorageUtils.UnanchorShipFromPlanet(ship, request.Departure);

            // 创建单位的尾迹，并挂载到星球上
            var trail = world.Construct(_trailTemplate.Archetype);
            _trailTemplate.Apply(trail);
            world.Create(new TrailOf(ship.Entity.Reference(), trail.Reference()));
            world.Create(new TreeRelationship<TrailOf>());
            var relativeTransformIdx = world.Create(
                new TreeRelationship<RelativeTransform>(ship.Entity.Reference(), trail.Reference()),
                new RelativeTransform());
            world.Create(new Dependence(trail.Reference(), ship));
        }

        // 移除任务
        _commandBuffer.Destroy(in requestEntity);
    }

    public override void Update(in GameTime t)
    {
        StartShippingQuery(World);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}

[CoreUpdateSystem]
public sealed partial class UpdateShipStateSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _delayTime = 0.5f;

    [Query]
    [All<ShippingTask, ShippingState>]
    private static void Proceed([Data] GameTime time, in ShippingTask task, ref ShippingState state)
    {
        state.TravelledTime += (float)time.ElapsedGameTime.TotalSeconds;
        state.Progress = MathF.Max((state.TravelledTime - _delayTime) / (task.ExpectedTravelDuration - _delayTime), 0);
    }
}

[StructuralChangeSystem]
public sealed partial class LandArrivedShipsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly List<Action> _actionBuffer = [];

    [Query]
    [All<ShippingTask, ShippingState>]
    private static void FindArrivedShips(Entity ship, ref ShippingState state, [Data] List<Action> actionBuffer)
    {
        if (state.Progress < 1)
            return;

        actionBuffer.Add(() =>
        {
            var task = ship.Get<ShippingTask>();

            // 将单位挂载到目标星球
            var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(ship, task.DestinationPlanet);
            transformRelationship.Set(task.ExpectedRevolutionOrbit, task.ExpectedRevolutionState);
            ship.Remove<ShippingTask, ShippingState>();

            // 销毁单位的尾迹实体
            var world = World.Worlds[ship.WorldId];
            world.Destroy(ship.Get<TrailOf.AsShip>().Index.TrailRef);
        });
    }

    public override void Update(in GameTime t)
    {
        FindArrivedShipsQuery(World, _actionBuffer);

        foreach (var action in _actionBuffer)
            action.Invoke();
        _actionBuffer.Clear();
    }
}

/// <summary>
/// 运输系统。根据运输时间计算单位动画、位置和方向
/// </summary>
[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteBefore(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class CalculateShipPositionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingTask, ShippingState, AbsoluteTransform>]
    private static void CalculatePosition(in ShippingTask task, in ShippingState state, ref AbsoluteTransform pose)
    {
        pose.Translation = Vector3.Lerp(task.DeparturePosition, task.ExpectedArrivalPosition, state.Progress);

        // 摆放尾向
        // 旋转后的+X轴指向目标点, XZ平面与原XY平面垂直
        var headX = Vector3.Normalize(task.ExpectedArrivalPosition - task.DeparturePosition);
        var headY = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, headX));
        var headZ = Vector3.Normalize(Vector3.Cross(headX, headY));
        var rotation = new Matrix { Right = headX, Up = headY, Backward = headZ };
        pose.Rotation = Quaternion.CreateFromRotationMatrix(rotation);
    }
}

[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteAfter(typeof(IndexTrailAffiliationSystem))]
public sealed partial class UpdateShippingEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _landDuration = 0.5f;

    private readonly AnimationClip<Entity> _trailStretchingAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/TrailStretching.json");

    private readonly AnimationClip<Entity> _trailExtinguishedAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/TrailExtinguished.json");

    private const float _delayDuration = 0.5f;
    private const float _takeOffDuration = 0.3f;

    private readonly AnimationClip<Entity> _unitBlinkingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBlinking.json");

    private readonly AnimationClip<Entity> _unitShippingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitShipping.json");

    private readonly AnimationClip<Entity> _unitTakingOffAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitTakingOff.json");

    [Query]
    [All<TrailOf.AsShip, ShippingTask, ShippingState, Sprite, Animation>]
    private void CalculateAnimation(in TrailOf.AsShip asShip,
                                    in ShippingTask shippingTask, in ShippingState shippingState, in Sprite sprite,
                                    ref Animation animation)
    {
        // 处理自己的动画
        if (animation.State == AnimationState.Clip)
        {
            if (animation.Clip.Clip == _unitBlinkingAnimationClip)
            {
                if (shippingState.TravelledTime <= _takeOffDuration)
                    animation.TriggerTransition(_unitTakingOffAnimationClip, _takeOffDuration);
            }
            else if (animation.Clip.Clip == _unitTakingOffAnimationClip)
            {
                animation.TriggerTransition(_unitShippingAnimationClip, _delayDuration - _takeOffDuration);
            }
            else if (animation.Clip.Clip == _unitShippingAnimationClip)
            {
                if (shippingTask.ExpectedTravelDuration - shippingState.TravelledTime <= _landDuration / 2)
                    animation.TriggerTransition(_unitBlinkingAnimationClip, _landDuration / 2);
            }
        }

        // 处理尾迹效果
        var trail = asShip.Index.TrailRef;
        
        // 尾迹的颜色和单位的颜色相同
        Debug.Assert(trail.Entity.Has<Sprite>());
        trail.Entity.Get<Sprite>().Color = sprite.Color;
        
        // 处理尾迹动画
        Debug.Assert(trail.Entity.Has<Animation>());
        ref var trailAnimation = ref trail.Entity.Get<Animation>();
        if (trailAnimation.State == AnimationState.Clip)
        {
            if (trailAnimation.Clip.Clip == _trailStretchingAnimation)
            {
                // 当单位快要结束时，切换进入熄灭状态
                if (shippingTask.ExpectedTravelDuration - shippingState.TravelledTime <= _landDuration)
                    trailAnimation.TriggerTransition(_trailExtinguishedAnimation, _landDuration);
            }
        }
    }
}
