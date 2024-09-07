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
        using var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;

            // 添加运输任务
            ship.Entity.Add<ShippingTask, ShippingStatus>();
            ref var shippingTask = ref ship.Entity.Get<ShippingTask>();
            ref var shippingState = ref ship.Entity.Get<ShippingStatus>();

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
            // 初始化状态
            shippingState.State = ShippingState.Charging;
            shippingState.Charging.ElapsedTime = 0;

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
public sealed partial class UpdateShipsStateSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingStatus>]
    private static void Proceed([Data] GameTime time, ref ShippingStatus status)
    {
        if (status.State == ShippingState.Charging)
            status.Charging.ElapsedTime += (float)time.ElapsedGameTime.TotalSeconds;
        else
            status.Travelling.ElapsedTime += (float)time.ElapsedGameTime.TotalSeconds;
    }
}

[LateUpdateSystem]
public sealed partial class FinishShipsChargingSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _chargingTime = 0.5f;

    [Query]
    [All<ShippingStatus>]
    private static void Proceed(ref ShippingStatus status)
    {
        if (status.State != ShippingState.Charging) return;

        if (status.Charging.ElapsedTime > _chargingTime)
        {
            status.State = ShippingState.Travelling;
            status.Travelling = new ShippingStatus_Travelling()
            {
                DelayedTime = status.Charging.ElapsedTime,
                ElapsedTime = 0,
            };
        }
    }
}

[StructuralChangeSystem]
[ExecuteAfter(typeof(StartShippingSystem))]
public sealed partial class LandArrivedShipsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly List<Entity> _arrivedEntities = [];

    [Query]
    [All<ShippingTask, ShippingStatus>]
    private static void FindArrivedShips(Entity ship, in ShippingTask task, ref ShippingStatus status,
                                         [Data] List<Entity> arrivedEntities)
    {
        if (status.State != ShippingState.Travelling) return;

        if (status.Travelling.ElapsedTime + status.Travelling.DelayedTime >= task.ExpectedTravelDuration)
            arrivedEntities.Add(ship);
    }

    private static void LandShip(Entity ship, in ShippingTask task, in ShippingStatus status)
    {
        // 将单位挂载到目标星球
        var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(ship, task.DestinationPlanet);
        transformRelationship.Set(task.ExpectedRevolutionOrbit, task.ExpectedRevolutionState);

        // 结束飞行。此后不能再访问task和status
        ship.Remove<ShippingTask, ShippingStatus>();

        // 销毁单位的尾迹实体
        var world = World.Worlds[ship.WorldId];
        world.Destroy(ship.Get<TrailOf.AsShip>().Index.TrailRef);
    }

    public override void Update(in GameTime t)
    {
        FindArrivedShipsQuery(World, _arrivedEntities);

        foreach (var entity in _arrivedEntities)
        {
            var refs = entity.Get<ShippingTask, ShippingStatus>();
            LandShip(entity, in refs.t0, in refs.t1);
        }
        _arrivedEntities.Clear();
    }
}

/// <summary>
/// 运输系统。根据运输时间计算单位动画、位置和方向
/// </summary>
[LateUpdateSystem]
[ExecuteAfter(typeof(AnimateSystem))]
[ExecuteBefore(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class CalculateShipPositionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingTask, ShippingStatus, AbsoluteTransform>]
    private static void CalculatePosition(in ShippingTask task, in ShippingStatus status, ref AbsoluteTransform pose)
    {
        if (status.State == ShippingState.Charging)
            pose.Translation = task.DeparturePosition;
        else if (status.State == ShippingState.Travelling)
        {
            var progress = (status.Travelling.ElapsedTime + status.Travelling.DelayedTime) /
                           task.ExpectedTravelDuration;
            pose.Translation = Vector3.Lerp(task.DeparturePosition, task.ExpectedArrivalPosition, progress);
        }

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
[ExecuteAfter(typeof(ApplyUnitPostBornEffectSystem))]
[ExecuteAfter(typeof(AnimateSystem))]
[ExecuteAfter(typeof(IndexTrailAffiliationSystem))]
public sealed partial class UpdateShippingEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _landDuration = 0.5f;
    private const float _takeOffDuration = 0.5f;

    private const float _unitShippingFadeInDuration = 0.1f;
    private const float _unitShippingFadeOutDuration = _landDuration / 2;

    private readonly AnimationClip<Entity> _unitShippingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitShipping.json");

    private readonly AnimationClip<Entity> _unitTakingOffAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitTakingOff.json");

    private readonly AnimationClip<Entity> _trailStretchingAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/TrailStretching.json");

    private readonly AnimationClip<Entity> _trailExtinguishedAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/TrailExtinguished.json");

    [Query]
    [All<TrailOf.AsShip, ShippingTask, ShippingStatus, Sprite, Animation>]
    private void CalculateAnimation(Entity ship, in TrailOf.AsShip asShip,
                                    in ShippingTask shippingTask, in ShippingStatus shippingStatus, in Sprite sprite,
                                    ref Animation animation)
    {
        // Charging状态下播放起飞动画
        if (shippingStatus.State == ShippingState.Charging)
        {
            var takingOffAnimationTime = shippingStatus.Charging.ElapsedTime;
            var fadeInTime = shippingStatus.Charging.ElapsedTime;
            var fadeInRatio = fadeInTime / _unitShippingFadeInDuration;

            switch (fadeInRatio)
            {
                case >= 0 and < 1:
                    AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                           null, float.NaN, // 上一个动画设置为空，直接继承上一个系统设置的值
                                                           _unitTakingOffAnimationClip, takingOffAnimationTime,
                                                           null, fadeInRatio); // 采用默认的线性差值
                    break;
                case >= 1:
                    AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitTakingOffAnimationClip,
                                                              takingOffAnimationTime);
                    break;
            }
        }
        // Travelling状态下播放飞行动画
        else if (shippingStatus.State == ShippingState.Travelling)
        {
            var shippingAnimationTime = shippingStatus.Travelling.ElapsedTime;
            var fadeOutTime = shippingStatus.Travelling.ElapsedTime -
                              (shippingTask.ExpectedTravelDuration - _unitShippingFadeOutDuration);
            var fadeOutRatio = fadeOutTime / _unitShippingFadeOutDuration;

            switch (fadeOutRatio)
            {
                case < 0:
                    AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitShippingAnimationClip,
                                                              shippingAnimationTime);
                    break;
                case >= 0 and < 1:
                    AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                           _unitShippingAnimationClip, shippingAnimationTime,
                                                           null, float.NaN, // 下一个动画设置为空，直接继承上一个系统设置的值
                                                           null, fadeOutRatio); // 采用默认的线性差值
                    break;
            }
        }

        // 处理尾迹效果
        var trail = asShip.Index.TrailRef.Entity;

        // 尾迹的颜色和单位的颜色相同
        Debug.Assert(trail.Has<Sprite>());
        trail.Get<Sprite>().Color = sprite.Color;

        // 应用尾迹动画
        if (shippingStatus.State == ShippingState.Travelling)
        {
            if (shippingStatus.Travelling.ElapsedTime < shippingTask.ExpectedTravelDuration - _landDuration)
            {
                var stretchingAnimationTime = shippingStatus.Travelling.ElapsedTime;
                AnimationEvaluator<Entity>.EvaluateAndSet(ref trail, _trailStretchingAnimation,
                                                          stretchingAnimationTime);
            }
            else
            {
                var stretchingAnimationTime = shippingStatus.Travelling.ElapsedTime;
                var crossTime = shippingStatus.Travelling.ElapsedTime -
                                (shippingTask.ExpectedTravelDuration - _landDuration);
                var crossRatio = crossTime / _landDuration;

                // 此处不是淡出，而只是单纯地用多个动画的交融构造效果
                AnimationEvaluator<Entity>.TweenAndSet(ref trail,
                                                       _trailStretchingAnimation, stretchingAnimationTime,
                                                       _trailExtinguishedAnimation, crossTime,
                                                       null, crossRatio); // 采用默认的线性差值
            }
        }
    }
}
