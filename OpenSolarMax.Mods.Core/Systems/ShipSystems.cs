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
[ExecuteBefore(typeof(ManageDependenceSystem))]
public sealed partial class StartShippingSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();
    private readonly UnitTrailTemplate _trailTemplate = new(assets);

    [Query]
    [All<StartShippingRequest>]
    private void StartShipping(Entity requestEntity, in StartShippingRequest request)
    {
        Debug.Assert(requestEntity.WorldId == request.Departure.WorldId
                     && requestEntity.WorldId == request.Destination.WorldId
                     && requestEntity.WorldId == request.Party.WorldId);

        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Get<AnchoredShipsRegistry>().Ships[request.Party];

        var shippable = request.Party.Get<Shippable>();
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
            ship.Add<ShippingTask, ShippingState>();
            ref var shippingTask = ref ship.Get<ShippingTask>();
            ref var shippingState = ref ship.Get<ShippingState>();

            // 获取相关信息
            ref readonly var pose = ref ship.Get<AbsoluteTransform>();
            var transformRelationship = ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Index.Relationship;
            ref readonly var revolutionOrbit = ref transformRelationship.Entity.Get<RevolutionOrbit>();
            ref readonly var revolutionState = ref transformRelationship.Entity.Get<RevolutionState>();
            ref readonly var departurePlanetOrbit = ref request.Departure.Get<PlanetGeostationaryOrbit>();
            ref readonly var destinationPlanetOrbit = ref request.Destination.Get<PlanetGeostationaryOrbit>();

            // 计算泊入轨道
            var orbitOffset = revolutionOrbit.Shape.Width / 2 / departurePlanetOrbit.Radius;
            var expectedOrbit = new RevolutionOrbit()
            {
                Rotation = destinationPlanetOrbit.Rotation,
                Shape = new(destinationPlanetOrbit.Radius * orbitOffset * 2, destinationPlanetOrbit.Radius * orbitOffset * 2),
                Period = destinationPlanetOrbit.Period * MathF.Pow(orbitOffset, 1.5f)
            };
            var expectedPosition = expectedArrivalPlanetPosition
                                   + RevolutionUtils.CalculateTransform(in expectedOrbit, in revolutionState).Translation;

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
            world.Create(new TrailOf(ship.Reference(), trail.Reference()));
            world.Create(new TreeRelationship<TrailOf>());
            var relativeTransformIdx = world.Create(
                new TreeRelationship<RelativeTransform>(ship.Reference(), trail.Reference()),
                new RelativeTransform());
            world.Create(new TreeRelationship<Party>(request.Party.Reference(), trail.Reference()));
            world.Create(new Dependence(trail.Reference(), ship.Reference()));

            // 摆放尾迹方向
            // 旋转后的+X轴指向目标点, XZ平面与原XY平面垂直
            var headX = Vector3.Normalize(shippingTask.ExpectedArrivalPosition - shippingTask.DeparturePosition);
            var headY = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, headX));
            var headZ = Vector3.Normalize(Vector3.Cross(headX, headY));
            var rotation = new Matrix { Right = headX, Up = headY, Backward = headZ };
            relativeTransformIdx.Get<RelativeTransform>().Rotation = Quaternion.CreateFromRotationMatrix(rotation);
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
    [Query]
    [All<ShippingTask, ShippingState>]
    private static void Proceed([Data] GameTime time, in ShippingTask task, ref ShippingState state)
    {
        state.TravelledTime += (float)time.ElapsedGameTime.TotalSeconds;
        state.Progress = state.TravelledTime / task.ExpectedTravelDuration;
    }
}

[StructuralChangeSystem]
[ExecuteBefore(typeof(ManageDependenceSystem))]
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
    }
}

[StructuralChangeSystem]
[ExecuteAfter(typeof(LandArrivedShipsSystem))]
[ExecuteBefore(typeof(StartShippingSystem))] // TODO - 此处是为了避免新创造的关系实体还没有被索引就被处理。需要更优雅的实现方法
[ExecuteBefore(typeof(ManageDependenceSystem))]
[ExecuteBefore(typeof(DestroyBrokenPartyRelationshipSystem))]
[ExecuteBefore(typeof(DestroyBrokenTrailRelationshipSystem))]
public sealed partial class ShipTrailLifecycleSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<TrailOf.AsTrail>]
    private void ManageLifecycle(Entity trail, in TrailOf.AsTrail asTrail)
    {
        var unit = asTrail.Index.ShipRef.Entity;

        if (!unit.Has<ShippingTask, ShippingState>())
            _commandBuffer.Destroy(trail);
    }

    public override void Update(in GameTime t)
    {
        ManageLifecycleQuery(World);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}

[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteAfter(typeof(IndexTrailAffiliationSystem))]
public sealed partial class ShipTrailEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _extinguishTime = 0.5f;

    private readonly AnimationClip<Entity> _stretchingAnimation = assets.Load<AnimationClip<Entity>>("Animations/TrailStretching.json");
    private readonly AnimationClip<Entity> _extinguishedAnimation = assets.Load<AnimationClip<Entity>>("Animations/TrailExtinguished.json");

    [Query]
    [All<TrailOf.AsTrail, Animation>]
    private void CalculateAnimation(in TrailOf.AsTrail asTrail, ref Animation animation)
    {
        var unit = asTrail.Index.ShipRef.Entity;
        ref readonly var unitShippingTask = ref unit.Get<ShippingTask>();
        ref readonly var unitShippingState = ref unit.Get<ShippingState>();

        if (animation.Clip == _stretchingAnimation)
        {
            // 当单位快要结束时，切换进入熄灭状态
            if (unitShippingTask.ExpectedTravelDuration - unitShippingState.TravelledTime <= _extinguishTime)
            {
                animation.Transition = new()
                {
                    PreviousClip = animation.Clip,
                    PreviousClipTime = animation.LocalTime,
                    Duration = _extinguishTime,
                    Tweener = null
                };
                animation.Clip = _extinguishedAnimation;
                animation.LocalTime = 0;
            }
        }
    }
}
