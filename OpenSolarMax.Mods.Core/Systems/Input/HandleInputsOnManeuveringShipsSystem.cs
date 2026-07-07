using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

[InputSystem, BeforeStructuralChanges]
[
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(InputFocusState)),
    ReadCurr(typeof(FleetSliderWidget)),
    ReadCurr(typeof(ReachabilityRegistry)),
    ReadCurr(typeof(Projection)),
    Iterate(typeof(JumpingStatus)),
    ReadCurr(typeof(PlanetSelectionRing.AsRing)),
    ReadCurr(typeof(ViewSelectionRing.AsRing)),
    ChangeStructure
]
public sealed partial class HandleInputsOnManeuveringShipsSystem(
    World world,
    IConceptFactory factory,
    IAssetsManager assets,
    [Section("systems:input:maneuvering")] IConfiguration configs
) : ICalcSystemWithStructuralChanges
{
    private readonly int _minimalSelectPixels = configs.RequireValue<int>("minimal_select_pixels");
    private ButtonState _lastLeftButton = ButtonState.Released;

    /// <summary>
    /// 淡出动画剪辑，用于选择圈消失时的动画。
    /// </summary>
    private readonly AnimationClip<Entity> _fadeOutClip = assets.Load<AnimationClip<Entity>>(
        "/Animations/SelectionRingFadeOut.json"
    );

    /// <summary>
    /// 获取某个星球在某个视图的选择圈实体。
    /// </summary>
    private Entity? GetSelectionRingForPlanet(Entity view, Entity planet)
    {
        if (!planet.Has<PlanetSelectionRing.AsPlanet>())
            return null;

        foreach (var (_, record) in planet.Get<PlanetSelectionRing.AsPlanet>().Relationships)
        {
            var ring = record.Ring;
            if (
                ring.Has<ViewSelectionRing.AsRing>()
                && ring.Get<ViewSelectionRing.AsRing>().Relationship?.Copy.View == view
            )
            {
                return ring;
            }
        }
        return null;
    }

    /// <summary>
    /// 为某个星球在某个视图创建选择圈实体。
    /// </summary>
    private void CreateSelectionRing(Entity view, Entity planet, CommandBuffer commandBuffer)
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.SelectionRing,
            new SelectionRingDescription() { Planet = planet, View = view }
        );
    }

    /// <summary>
    /// 为选择圈设置淡出动画。
    /// </summary>
    private void SetFadeOutAnimation(Entity ring, CommandBuffer commandBuffer)
    {
        // 重置动画状态并设置淡出剪辑，播完后由 ExpireAnimationCompletedEntitiesSystem 自动销毁
        commandBuffer.Set(in ring, new Animation() { Clip = _fadeOutClip });
    }

    /// <summary>
    /// 直接销毁选择圈实体。
    /// </summary>
    private void DestroySelectionRing(Entity ring, CommandBuffer commandBuffer)
    {
        commandBuffer.Destroy(ring);
    }

    /// <summary>
    /// 为视图的所有选择圈设置淡出动画（用于吸附松开和右键发送飞船）。
    /// </summary>
    private void FadeOutAllSelectionRings(Entity view, CommandBuffer commandBuffer)
    {
        if (!view.Has<ViewSelectionRing.AsView>())
            return;

        foreach (var (_, record) in view.Get<ViewSelectionRing.AsView>().Relationships)
        {
            SetFadeOutAnimation(record.Ring, commandBuffer);
        }
    }

    /// <summary>
    /// 销毁视图的所有选择圈（用于悬空松开）。
    /// </summary>
    private void DestroyAllSelectionRings(Entity view, CommandBuffer commandBuffer)
    {
        if (!view.Has<ViewSelectionRing.AsView>())
            return;

        foreach (var (_, record) in view.Get<ViewSelectionRing.AsView>().Relationships)
        {
            DestroySelectionRing(record.Ring, commandBuffer);
        }
    }

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
    private void CheckPointedPlanet(
        Entity planet,
        in AbsoluteTransform pose,
        [Data] in Point mouseInScreen,
        [Data] in Matrix worldToScreen,
        [Data] ref Entity pointedPlanet,
        [Data] ref float pointedPlanetZ
    )
    {
        float radiusInScreen = _minimalSelectPixels;
        if (planet.Has<ReferenceSize>()) // 若对象没有参考尺寸，则按照最小选择像素数判定范围；否则按照参考尺寸判定，但不得小于最小值
        {
            ref readonly var refSize = ref planet.Get<ReferenceSize>();
            var halfSizeInScreen = Vector2.TransformNormal(new(refSize.Radius), worldToScreen);
            radiusInScreen = MathF.Max(
                MathF.Abs(MathF.MaxMagnitude(halfSizeInScreen.X, halfSizeInScreen.Y)),
                radiusInScreen
            );
        }

        var positionInScreen = Vector3.Transform(pose.Translation, worldToScreen);
        var delta = new Vector2(
            positionInScreen.X - mouseInScreen.X,
            positionInScreen.Y - mouseInScreen.Y
        );
        if (delta.Length() < radiusInScreen && positionInScreen.Z < pointedPlanetZ)
        {
            pointedPlanet = planet;
            pointedPlanetZ = positionInScreen.Z;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entity GetPointedPlanet(in Point mouseInScreen, in Matrix worldToScreen)
    {
        Entity pointedPlanet = Entity.Null;
        float pointedPlanetZ = float.PositiveInfinity;
        CheckPointedPlanetQuery(
            world,
            in mouseInScreen,
            in worldToScreen,
            ref pointedPlanet,
            ref pointedPlanetZ
        );
        return pointedPlanet;
    }

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
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

    private void HandleSelectionStateTransition(
        Entity view, // View 实体
        ref ShipsSelection selection,
        float percentage,
        in Matrix worldToScreen,
        Entity team,
        ref Entity? pointedPlanet,
        CommandBuffer commandBuffer,
        in InputFocusState focus
    )
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();

        var mouseInScreen = mouse.Position;

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Simple Selecting
        //
        if (selection.State == ShipsSelection_State.SimpleSelecting)
        {
            if (mouse.LeftButton == ButtonState.Pressed && focus.MouseFocused)
            {
                var tappingSource = pointedPlanet ??= GetPointedPlanet(
                    in mouseInScreen,
                    worldToScreen
                );
                var keepPrevious = keys[Keys.LeftShift] == KeyState.Down;

                if (tappingSource == Entity.Null)
                {
                    if (selection.SimpleSelecting.TappingSource != Entity.Null)
                    {
                        // 当前左键点在空白处，但是之前还正在点选一个星球，则切换至拖拽状态
                        selection.State = ShipsSelection_State.DraggingToDestination;
                        selection.DraggingToDestination = new()
                        {
                            SelectedSources = selection.SimpleSelecting.SelectedSources,
                        };
                    }
                    else if (_lastLeftButton == ButtonState.Released)
                    {
                        // 当前左键点在空白处，而且之前也没有正在点选任何星球，则切换至框选状态
                        selection.State = ShipsSelection_State.BoxSelectingSources;
                        selection.BoxSelectingSources = new()
                        {
                            BoxStartInScreen = mouseInScreen,
                            OtherSelectedPlanets = keepPrevious
                                ? selection.SimpleSelecting.SelectedSources
                                : [],
                        };
                    }
                }
            }
            else if (
                mouse.RightButton != ButtonState.Pressed
                && selection.SimpleSelecting.TappingDestination != Entity.Null
            )
            {
                // 当前右键没有按下，但是之前有选中的目标，则操作舰船，并切换至初始状态的简单选择状态
                foreach (var departure in selection.SimpleSelecting.SelectedSources)
                {
                    // 排除目标星球和出发星球相同的情况
                    if (departure == selection.SimpleSelecting.TappingDestination)
                        continue;

                    // 排除目标星球和出发星球之间被障碍物遮挡的情况
                    if (!CheckReachability(departure, selection.SimpleSelecting.TappingDestination))
                        continue;

                    factory.Make(
                        world,
                        commandBuffer,
                        new JumpingRequestDescription()
                        {
                            Departure = departure,
                            Destination = selection.SimpleSelecting.TappingDestination,
                            Team = team,
                            ExpectedNum = (int)
                                MathF.Round(
                                    departure.Get<AnchoredShipsRegistry>().Ships[team].Count()
                                        * percentage
                                ),
                        }
                    );
                }
                // 右键发送飞船后，给所有选择圈设置淡出动画
                FadeOutAllSelectionRings(view, commandBuffer);
                selection.State = ShipsSelection_State.SimpleSelecting;
                selection.SimpleSelecting = new() { SelectedSources = [] };
            }
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Box-Selecting
        //
        else if (selection.State == ShipsSelection_State.BoxSelectingSources)
        {
            if (mouse.LeftButton == ButtonState.Released)
            {
                selection.State = ShipsSelection_State.SimpleSelecting;
                selection.SimpleSelecting = new()
                {
                    SelectedSources = new(
                        Enumerable.Union(
                            selection.BoxSelectingSources.OtherSelectedPlanets,
                            selection.BoxSelectingSources.PlanetsInBox
                        )
                    ),
                };
            }
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Dragging to Destination
        //
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            if (mouse.LeftButton == ButtonState.Released)
            {
                selection.State = ShipsSelection_State.SimpleSelecting;

                if (selection.DraggingToDestination.CandidateDestination != Entity.Null)
                {
                    foreach (var departure in selection.DraggingToDestination.SelectedSources)
                    {
                        // 排除目标星球和出发星球相同的情况
                        if (departure == selection.DraggingToDestination.CandidateDestination)
                            continue;

                        // 排除目标星球和出发星球之间被障碍物遮挡的情况
                        if (
                            !CheckReachability(
                                departure,
                                selection.DraggingToDestination.CandidateDestination
                            )
                        )
                            continue;

                        factory.Make(
                            world,
                            commandBuffer,
                            new JumpingRequestDescription()
                            {
                                Departure = departure,
                                Destination = selection.DraggingToDestination.CandidateDestination,
                                Team = team,
                                ExpectedNum = (int)
                                    MathF.Round(
                                        departure.Get<AnchoredShipsRegistry>().Ships[team].Count()
                                            * percentage
                                    ),
                            }
                        );
                    }
                    // 吸附松开后，给所有选择圈设置淡出动画
                    FadeOutAllSelectionRings(view, commandBuffer);
                    selection.SimpleSelecting = new() { SelectedSources = [] };
                }
                else
                {
                    // 悬空松开后，直接销毁所有选择圈（原版 S2 行为）
                    DestroyAllSelectionRings(view, commandBuffer);
                    selection.SimpleSelecting = new() { SelectedSources = [] };
                }
            }
        }
    }

    private void UpdateSelectionStatus(
        Entity view, // View 实体
        ref ShipsSelection selection,
        in Matrix worldToScreen,
        Entity team,
        ref Entity? pointedPlanet,
        in InputFocusState focus,
        CommandBuffer commandBuffer
    )
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();

        var mouseInScreen = mouse.Position;

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Simple Selecting
        //
        if (selection.State == ShipsSelection_State.SimpleSelecting)
        {
            selection.SimpleSelecting.PointingPlanet = pointedPlanet ??= GetPointedPlanet(
                in mouseInScreen,
                worldToScreen
            );

            if (mouse.LeftButton == ButtonState.Pressed && focus.MouseFocused)
            {
                var tappingSource = selection.SimpleSelecting.PointingPlanet;
                selection.SimpleSelecting.TappingSource = selection.SimpleSelecting.PointingPlanet;
                if (_lastLeftButton == ButtonState.Released && tappingSource != Entity.Null)
                {
                    if (keys[Keys.LeftShift] != KeyState.Down)
                    {
                        // 清空之前的选中，需要先给所有选择圈设置淡出动画
                        FadeOutAllSelectionRings(view, commandBuffer);
                        selection.SimpleSelecting.SelectedSources.Clear();
                    }
                    selection.SimpleSelecting.SelectedSources.Add(tappingSource);
                    // 为新选中的星球创建选择圈（如果该星球还没有属于当前视图的选择圈）
                    if (GetSelectionRingForPlanet(view, tappingSource) == null)
                        CreateSelectionRing(view, tappingSource, commandBuffer);
                }
            }
            else
                selection.SimpleSelecting.TappingSource = Entity.Null;

            if (mouse.RightButton == ButtonState.Pressed && focus.MouseFocused)
            {
                // 将当前右键点选的对象，不论有没有点选在星球上，都进行记录
                var tappingDestination = selection.SimpleSelecting.PointingPlanet;
                selection.SimpleSelecting.TappingDestination = tappingDestination;
            }
            else
                selection.SimpleSelecting.TappingDestination = Entity.Null;
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Box-Selecting
        //
        else if (selection.State == ShipsSelection_State.BoxSelectingSources)
        {
            // 在框选状态下时，更新选框，并计算选框内的星球
            var boxOrigin = new Point(
                Math.Min(mouseInScreen.X, selection.BoxSelectingSources.BoxStartInScreen.X),
                Math.Min(mouseInScreen.Y, selection.BoxSelectingSources.BoxStartInScreen.Y)
            );
            var boxSize = new Point(
                Math.Abs(mouseInScreen.X - selection.BoxSelectingSources.BoxStartInScreen.X),
                Math.Abs(mouseInScreen.Y - selection.BoxSelectingSources.BoxStartInScreen.Y)
            );
            selection.BoxSelectingSources.BoxInScreen = new(boxOrigin, boxSize);
            selection.BoxSelectingSources.PlanetsInBox = GetBoxedPlanets(
                in selection.BoxSelectingSources.BoxInScreen,
                worldToScreen
            );
        }
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Dragging to Destination
        //
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            // 在拖拽状态下时，计算并记录当前拖拽到的目标星球
            var hoveringDestination = pointedPlanet ??= GetPointedPlanet(
                in mouseInScreen,
                worldToScreen
            );
            selection.DraggingToDestination.CandidateDestination = hoveringDestination;
        }
    }

    [Query]
    [All<
        ManeuveringShipsStatus,
        FleetSliderWidget,
        InTeam.AsAffiliate,
        Projection,
        InputFocusState
    >]
    private void HandleInputs(
        Entity entity, // View 实体
        ref ManeuveringShipsStatus status,
        in FleetSliderWidget fleetSlider,
        in InTeam.AsAffiliate ofTeam,
        in Projection projection,
        in InputFocusState focus,
        [Data] CommandBuffer commandBuffer
    )
    {
        Entity? pointedPlanet = null;
        HandleSelectionStateTransition(
            entity, // 传入 View 实体
            ref status.Selection,
            fleetSlider.Percentage,
            in projection.WorldToScreen,
            ofTeam.Relationship!.Value.Copy.Team,
            ref pointedPlanet,
            commandBuffer,
            in focus
        );
        UpdateSelectionStatus(
            entity, // 传入 View 实体
            ref status.Selection,
            in projection.WorldToScreen,
            ofTeam.Relationship!.Value.Copy.Team,
            ref pointedPlanet,
            in focus,
            commandBuffer
        );
        _lastLeftButton = Mouse.GetState().LeftButton;
    }

    public void Update(CommandBuffer commandBuffer) => HandleInputsQuery(world, commandBuffer);
}
