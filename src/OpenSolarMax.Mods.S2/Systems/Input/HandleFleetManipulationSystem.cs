using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 舰队操纵系统。维持视图上的操纵状态机，处理点选、框选、拖拽与右键派兵
/// </summary>
[LateUpdate]
[InputSystem]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(InTeam.AsAffiliate))]
[ReadCurr(typeof(InputFocusState))]
[ReadCurr(typeof(FleetSliderWidget))]
[ReadCurr(typeof(ReachabilityRegistry))]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(PointingStatus))]
[ReadCurr(typeof(InputGesture))]
[ReadCurr(typeof(MouseDeviceState))]
[ReadCurr(typeof(KeyboardDeviceState))]
[Calc(typeof(FleetManipulationStatus))]
[Calc(typeof(FleetManipulationSources))]
[DelayedCalc]
public sealed partial class HandleFleetManipulationSystem(
    World world,
    IConceptFactory factory,
    IAssetsManager assets
) : IDelayedCalcSystem
{
    /// <summary>扩散式淡出动画</summary>
    private readonly AnimationClip<Entity> _growFadeClip = assets.Load<AnimationClip<Entity>>(
        Content.Animations.SelectionRingFadeOut_json
    );

    /// <summary>收缩式淡出动画</summary>
    private readonly AnimationClip<Entity> _shrinkFadeClip = assets.Load<AnimationClip<Entity>>(
        Content.Animations.SelectionRingShrinkFadeOut_json
    );

    /// <summary>
    /// 为某个星球在某个视图创建选择圈实体，返回创建的实体
    /// </summary>
    private Entity CreateSelectionRing(Entity view, Entity planet, CommandBuffer commandBuffer) =>
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.SelectionRing,
            new SelectionRingDescription() { Planet = planet, View = view }
        );

    private void SetFadeAnimation(
        Entity ring,
        AnimationClip<Entity> clip,
        CommandBuffer commandBuffer
    ) => commandBuffer.Set(in ring, new Animation() { Clip = clip });

    /// <summary>
    /// 为一组星球创建扩散式淡出圈
    /// </summary>
    private void CreateAndFadeOutSelectionRings(
        Entity view,
        IEnumerable<Entity> planets,
        CommandBuffer commandBuffer
    )
    {
        foreach (var planet in planets)
        {
            var ring = CreateSelectionRing(view, planet, commandBuffer);
            SetFadeAnimation(ring, _growFadeClip, commandBuffer);
        }
    }

    [Query]
    [All<Pickable, AbsoluteTransform>]
    private static void CheckBoxedPlanets(
        Entity planet,
        in AbsoluteTransform pose,
        [Data] in Rectangle box,
        [Data] in Matrix worldToScreen,
        [Data] ref HashSet<Entity> boxedPlanets
    )
    {
        var positionInScreen = Vector3.Transform(pose.Translation, worldToScreen);
        if (box.Contains(positionInScreen.X, positionInScreen.Y))
            boxedPlanets.Add(planet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HashSet<Entity> GetBoxedPlanets(in Rectangle box, in Matrix worldToScreen)
    {
        var boxedPlanets = new HashSet<Entity>();
        CheckBoxedPlanetsQuery(world, in box, in worldToScreen, ref boxedPlanets);
        return boxedPlanets;
    }

    private bool CheckReachability(Entity departure, Entity destination)
    {
        return departure.Get<ReachabilityRegistry>().FromHereTo[destination];
    }

    /// <summary>
    /// 派兵。对每个满足条件的出发星球创建跳跃请求，并为出发星球生成扩散淡出圈、为目标星球生成收缩淡出圈
    /// </summary>
    private void DispatchFleets(
        Entity view,
        IEnumerable<Entity> departures,
        Entity destination,
        Entity team,
        float percentage,
        CommandBuffer commandBuffer
    )
    {
        foreach (var departure in departures)
        {
            // 排除目标星球和出发星球相同的情况
            if (departure == destination)
                continue;

            // 排除目标星球和出发星球之间被障碍物遮挡的情况
            if (!CheckReachability(departure, destination))
                continue;

            // 排除选兵数量为零的情况
            var expectedNum = (int)
                MathF.Round(
                    departure.Get<AnchoredShipsRegistry>().Ships[team].Count() * percentage
                );
            if (expectedNum == 0)
                continue;

            factory.Make(
                world,
                commandBuffer,
                new JumpingRequestDescription()
                {
                    Departure = departure,
                    Destination = destination,
                    Team = team,
                    ExpectedNum = expectedNum,
                }
            );
        }

        CreateAndFadeOutSelectionRings(view, departures, commandBuffer);
        var destinationRing = CreateSelectionRing(view, destination, commandBuffer);
        SetFadeAnimation(destinationRing, _shrinkFadeClip, commandBuffer);
    }

    /// <summary>
    /// 把候选提交进选中集合。按住 Shift 时追加，否则替换；离开集合的天体生成淡出圈
    /// </summary>
    private void CommitSelection(
        Entity view,
        ref FleetManipulationSources sources,
        IEnumerable<Entity> candidates,
        bool keepPrevious,
        CommandBuffer commandBuffer
    )
    {
        var next = keepPrevious ? new HashSet<Entity>(sources.Departures) : [];
        next.UnionWith(candidates);

        var leaving = sources.Departures.Where(departure => !next.Contains(departure)).ToList();
        CreateAndFadeOutSelectionRings(view, leaving, commandBuffer);

        sources.Departures.Clear();
        sources.Departures.UnionWith(next);
    }

    private void HandleStateTransition(
        Entity view,
        ref FleetManipulationStatus status,
        ref FleetManipulationSources sources,
        float percentage,
        Entity team,
        CommandBuffer commandBuffer,
        in InputFocusState focus,
        in PointingStatus pointing,
        in InputGesture gesture,
        in MouseDeviceState mouseDevice,
        in KeyboardDeviceState keyboardDevice
    )
    {
        var mouse = mouseDevice.State;
        var previousMouse = mouseDevice.PreviousState;
        var keepPrevious = keyboardDevice.State[Keys.LeftShift] == KeyState.Down;

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // 简单选择
        //
        if (status.State.TryPickT0(out var simpleSelecting, out _))
        {
            if (mouse.LeftButton == ButtonState.Pressed && focus.MouseFocused)
            {
                if (gesture.State.TryPickT2(out _, out _) && pointing.PointedEntity is null)
                {
                    if (simpleSelecting.CandidateSource is { } candidateSource)
                    {
                        // 拖拽手势成立时提交候选，之后拖拽状态只依赖集合
                        CommitSelection(
                            view,
                            ref sources,
                            [candidateSource],
                            keepPrevious,
                            commandBuffer
                        );
                        status.State = new FleetManipulation_DraggingToDestination();
                    }
                    else
                    {
                        // 当前拖拽手势指向空白处，而且之前也没有按在任何天体上，则切换至框选状态
                        status.State = new FleetManipulation_BoxSelectingSources();
                    }
                }
            }
            else if (
                previousMouse.LeftButton == ButtonState.Pressed
                && mouse.LeftButton == ButtonState.Released
            )
            {
                // 松开时提交候选；没有候选则按点击空白处理
                if (simpleSelecting.CandidateSource is { } candidateSource)
                    CommitSelection(
                        view,
                        ref sources,
                        [candidateSource],
                        keepPrevious,
                        commandBuffer
                    );
                else
                    CommitSelection(view, ref sources, [], keepPrevious, commandBuffer);

                status.State = new FleetManipulation_SimpleSelecting();
            }
            else if (
                mouse.RightButton != ButtonState.Pressed
                && simpleSelecting.CandidateDestination is { } candidateDestination
            )
            {
                // 当前右键没有按下，但是之前有瞄准的目标，则派兵，并回到初始的简单选择状态
                DispatchFleets(
                    view,
                    sources.Departures,
                    candidateDestination,
                    team,
                    percentage,
                    commandBuffer
                );
                sources.Departures.Clear();
                status.State = new FleetManipulation_SimpleSelecting();
            }
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // 框选
        //
        else if (status.State.TryPickT1(out var boxSelecting, out _))
        {
            if (mouse.LeftButton == ButtonState.Released)
            {
                CommitSelection(
                    view,
                    ref sources,
                    boxSelecting.PlanetsInBox,
                    keepPrevious,
                    commandBuffer
                );
                status.State = new FleetManipulation_SimpleSelecting();
            }
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // 拖拽至目标
        //
        else if (status.State.TryPickT2(out _, out _))
        {
            if (mouse.LeftButton == ButtonState.Released)
            {
                if (pointing.PointedEntity is { } destination)
                {
                    DispatchFleets(
                        view,
                        sources.Departures,
                        destination,
                        team,
                        percentage,
                        commandBuffer
                    );
                    sources.Departures.Clear();
                }
                status.State = new FleetManipulation_SimpleSelecting();
            }
        }
    }

    private void RefreshTransientFields(
        Entity view,
        ref FleetManipulationStatus status,
        ref FleetManipulationSources sources,
        in Matrix worldToScreen,
        in InputFocusState focus,
        CommandBuffer commandBuffer,
        in PointingStatus pointing,
        in InputGesture gesture,
        in MouseDeviceState mouseDevice,
        in KeyboardDeviceState keyboardDevice
    )
    {
        var mouse = mouseDevice.State;
        var previousMouse = mouseDevice.PreviousState;

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // 简单选择
        //
        if (status.State.TryPickT0(out var simpleSelecting, out _))
        {
            // 只在按下的那一帧记录候选来源，按住期间不跟随指针
            if (
                previousMouse.LeftButton == ButtonState.Released
                && mouse.LeftButton == ButtonState.Pressed
                && focus.MouseFocused
                && pointing.PointedEntity is { } candidateSource
            )
                simpleSelecting.CandidateSource = candidateSource;

            // 松开后清空候选来源
            if (
                previousMouse.LeftButton == ButtonState.Pressed
                && mouse.LeftButton == ButtonState.Released
            )
                simpleSelecting.CandidateSource = null;

            // 右键按住时，目标候选跟随指针
            if (mouse.RightButton == ButtonState.Pressed && focus.MouseFocused)
            {
                // 将当前右键点选的对象，不论有没有点选在星球上，都进行记录
                simpleSelecting.CandidateDestination = pointing.PointedEntity;
            }
            else
                simpleSelecting.CandidateDestination = null;

            status.State = simpleSelecting;
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // 框选
        //
        else if (status.State.TryPickT1(out var boxSelecting, out _))
        {
            // 在框选状态下时，选框矩形从拖拽手势的按下位置与当前位置计算，框内天体每帧刷新
            if (gesture.State.TryPickT2(out var dragging, out _))
            {
                var box = new Rectangle(
                    new Point(
                        Math.Min(dragging.CurrentPosition.X, dragging.PressedPosition.X),
                        Math.Min(dragging.CurrentPosition.Y, dragging.PressedPosition.Y)
                    ),
                    new Point(
                        Math.Abs(dragging.CurrentPosition.X - dragging.PressedPosition.X),
                        Math.Abs(dragging.CurrentPosition.Y - dragging.PressedPosition.Y)
                    )
                );
                boxSelecting.PlanetsInBox = GetBoxedPlanets(in box, in worldToScreen);
                status.State = boxSelecting;
            }
        }
    }

    [Query]
    [All<
        FleetManipulationStatus,
        FleetManipulationSources,
        FleetSliderWidget,
        InTeam.AsAffiliate,
        Projection,
        InputFocusState,
        PointingStatus,
        InputGesture,
        MouseDeviceState,
        KeyboardDeviceState
    >]
    private void HandleInputs(
        Entity entity, // View 实体
        ref FleetManipulationStatus status,
        ref FleetManipulationSources sources,
        in FleetSliderWidget fleetSlider,
        in InTeam.AsAffiliate ofTeam,
        in Projection projection,
        in InputFocusState focus,
        in PointingStatus pointing,
        in InputGesture gesture,
        in MouseDeviceState mouseDevice,
        in KeyboardDeviceState keyboardDevice,
        [Data] CommandBuffer commandBuffer
    )
    {
        var team = ofTeam.Relationship!.Value.Copy.Team;

        HandleStateTransition(
            entity, // 传入 View 实体
            ref status,
            ref sources,
            fleetSlider.Percentage,
            team,
            commandBuffer,
            in focus,
            in pointing,
            in gesture,
            in mouseDevice,
            in keyboardDevice
        );
        RefreshTransientFields(
            entity,
            ref status,
            ref sources,
            in projection.WorldToScreen,
            in focus,
            commandBuffer,
            in pointing,
            in gesture,
            in mouseDevice,
            in keyboardDevice
        );

        // 剔除已销毁的天体
        sources.Departures.RemoveWhere(planet => !planet.IsAlive());
    }

    public void Update(CommandBuffer commandBuffer) => HandleInputsQuery(world, commandBuffer);
}
