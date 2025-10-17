using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems;

[InputSystem, BeforeStructuralChanges]
[ReadCurr(typeof(Camera)), ReadCurr(typeof(AbsoluteTransform)), ReadCurr(typeof(InParty.AsAffiliate)),
 ReadCurr(typeof(ReachabilityRegistry))]
[Iterate(typeof(ShippingStatus)), ChangeStructure]
public sealed partial class HandleInputsOnManeuveringShipsSystem(World world) : ICalcSystemWithStructuralChanges
{
    private const int _minimalSelectPixels = 10;

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
    private static void CheckPointedPlanet(Entity planet, in AbsoluteTransform pose,
                                           [Data] in Point mouseInViewport, [Data] in Matrix worldToViewport,
                                           [Data] ref Entity pointedPlanet, [Data] ref float pointedPlanetZ)
    {
        float radiusInViewport = _minimalSelectPixels;
        if (planet.Has<ReferenceSize>()) // 若对象没有参考尺寸，则按照最小选择像素数判定范围；否则按照参考尺寸判定，但不得小于最小值
        {
            ref readonly var refSize = ref planet.Get<ReferenceSize>();
            var halfSizeInViewport = Vector2.TransformNormal(new(refSize.Radius), worldToViewport);
            radiusInViewport =
                MathF.Max(MathF.Abs(MathF.MaxMagnitude(halfSizeInViewport.X, halfSizeInViewport.Y)), radiusInViewport);
        }

        var positionInViewport = Vector3.Transform(pose.Translation, worldToViewport);
        var delta = new Vector2(positionInViewport.X - mouseInViewport.X, positionInViewport.Y - mouseInViewport.Y);
        if (delta.Length() < radiusInViewport && positionInViewport.Z < pointedPlanetZ)
        {
            pointedPlanet = planet;
            pointedPlanetZ = positionInViewport.Z;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entity GetPointedPlanet(in Point mouseInViewport, in Matrix worldToViewport)
    {
        Entity pointedPlanet = Entity.Null;
        float pointedPlanetZ = float.PositiveInfinity;
        CheckPointedPlanetQuery(world, in mouseInViewport, in worldToViewport, ref pointedPlanet, ref pointedPlanetZ);
        return pointedPlanet;
    }

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
    private static void CheckBoxedPlanets(Entity planet, in AbsoluteTransform pose,
                                          [Data] in Rectangle box, [Data] in Matrix worldToViewport,
                                          [Data] ref HashSet<Entity> boxedPlanets)
    {
        var positionInViewport = Vector3.Transform(pose.Translation, worldToViewport);
        if (box.Contains(positionInViewport.X, positionInViewport.Y))
            boxedPlanets.Add(planet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HashSet<Entity> GetBoxedPlanets(in Rectangle box, in Matrix worldToViewport)
    {
        var boxedPlanets = new HashSet<Entity>();
        CheckBoxedPlanetsQuery(world, in box, in worldToViewport, ref boxedPlanets);
        return boxedPlanets;
    }

    private bool CheckReachability(Entity departure, Entity destination)
    {
        return departure.Get<ReachabilityRegistry>().FromHereTo[destination];
    }

    private void HandleSelectionStateTransition(ref ShipsSelection selection,
                                                in Matrix worldToViewport, in Viewport viewport, Entity party,
                                                ref Entity? pointedPlanet, CommandBuffer commandBuffer)
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();

        var mouseInViewport = new Point(mouse.X - viewport.X, mouse.Y - viewport.Y);

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Simple Selecting
        //
        if (selection.State == ShipsSelection_State.SimpleSelecting)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var tappingSource = pointedPlanet ??= GetPointedPlanet(in mouseInViewport, worldToViewport);
                var keepPrevious = keys[Keys.LeftShift] == KeyState.Down;

                if (tappingSource == Entity.Null)
                {
                    if (selection.SimpleSelecting.TappingSource != Entity.Null)
                    {
                        // 当前左键点在空白处，但是之前还正在点选一个星球，则切换至拖拽状态
                        selection.State = ShipsSelection_State.DraggingToDestination;
                        selection.DraggingToDestination = new()
                            { SelectedSources = selection.SimpleSelecting.SelectedSources };
                    }
                    else
                    {
                        // 当前左键点在空白处，而且之前也没有正在点选任何星球，则切换至框选状态
                        selection.State = ShipsSelection_State.BoxSelectingSources;
                        selection.BoxSelectingSources = new()
                        {
                            BoxStartInViewport = mouseInViewport,
                            OtherSelectedPlanets = keepPrevious ? selection.SimpleSelecting.SelectedSources : []
                        };
                    }
                }
            }
            else if (mouse.RightButton != ButtonState.Pressed
                     && selection.SimpleSelecting.TappingDestination != Entity.Null)
            {
                // 当前右键没有按下，但是之前有选中的目标，则操作单位，并切换至初始状态的简单选择状态
                foreach (var departure in selection.SimpleSelecting.SelectedSources)
                {
                    // 排除目标星球和出发星球相同的情况
                    if (departure == selection.SimpleSelecting.TappingDestination)
                        continue;

                    // 排除目标星球和出发星球之间被障碍物遮挡的情况
                    if (!CheckReachability(
                            departure, selection.SimpleSelecting.TappingDestination))
                        continue;

                    world.Make(commandBuffer, new ShippingRequestTemplate()
                    {
                        Departure = departure,
                        Destination = selection.SimpleSelecting.TappingDestination,
                        Party = party,
                        ExpectedNum = departure.Get<AnchoredShipsRegistry>().Ships[party].Count()
                    });
                }
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
                    SelectedSources = new(Enumerable.Union(selection.BoxSelectingSources.OtherSelectedPlanets,
                                                           selection.BoxSelectingSources.PlanetsInBox))
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
                        if (!CheckReachability(
                                departure, selection.DraggingToDestination.CandidateDestination))
                            continue;

                        world.Make(commandBuffer, new ShippingRequestTemplate()
                        {
                            Departure = departure,
                            Destination = selection.DraggingToDestination.CandidateDestination,
                            Party = party,
                            ExpectedNum = departure.Get<AnchoredShipsRegistry>().Ships[party].Count()
                        });
                    }
                    selection.SimpleSelecting = new() { SelectedSources = [] };
                }
                else
                    selection.SimpleSelecting = new()
                        { SelectedSources = selection.DraggingToDestination.SelectedSources };
            }
        }
    }

    private void UpdateSelectionStatus(ref ShipsSelection selection,
                                       in Matrix worldToViewport, in Viewport viewport, Entity party,
                                       ref Entity? pointedPlanet)
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();

        var mouseInViewport = new Point(mouse.X - viewport.X, mouse.Y - viewport.Y);

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Simple Selecting
        //
        if (selection.State == ShipsSelection_State.SimpleSelecting)
        {
            selection.SimpleSelecting.PointingPlanet =
                pointedPlanet ??= GetPointedPlanet(in mouseInViewport, worldToViewport);

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var tappingSource = selection.SimpleSelecting.PointingPlanet;
                selection.SimpleSelecting.TappingSource = selection.SimpleSelecting.PointingPlanet;
                if (tappingSource != Entity.Null)
                {
                    if (keys[Keys.LeftShift] != KeyState.Down)
                        selection.SimpleSelecting.SelectedSources.Clear();
                    selection.SimpleSelecting.SelectedSources.Add(tappingSource);
                }
            }
            else
                selection.SimpleSelecting.TappingSource = Entity.Null;

            if (mouse.RightButton == ButtonState.Pressed)
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
            var boxOrigin = new Point(Math.Min(mouseInViewport.X, selection.BoxSelectingSources.BoxStartInViewport.X),
                                      Math.Min(mouseInViewport.Y, selection.BoxSelectingSources.BoxStartInViewport.Y));
            var boxSize = new Point(Math.Abs(mouseInViewport.X - selection.BoxSelectingSources.BoxStartInViewport.X),
                                    Math.Abs(mouseInViewport.Y - selection.BoxSelectingSources.BoxStartInViewport.Y));
            selection.BoxSelectingSources.BoxInViewport = new(boxOrigin, boxSize);
            selection.BoxSelectingSources.PlanetsInBox =
                GetBoxedPlanets(in selection.BoxSelectingSources.BoxInViewport, worldToViewport);
        }

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // Dragging to Destination
        //
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            // 在拖拽状态下时，计算并记录当前拖拽到的目标星球
            var hoveringDestination = pointedPlanet ??= GetPointedPlanet(in mouseInViewport, worldToViewport);
            selection.DraggingToDestination.CandidateDestination = hoveringDestination;
        }
    }

    [Query]
    [All<Camera, AbsoluteTransform, ManeuvaringShipsStatus, InParty.AsAffiliate>]
    private void HandleInputs(in Camera camera, in AbsoluteTransform pose, ref ManeuvaringShipsStatus status,
                              in InParty.AsAffiliate ofParty, [Data] CommandBuffer commandBuffer)
    {
        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var canvas = camera.Output.Bounds;
        var canvasToNdc = Matrix.CreateOrthographicOffCenter(0, canvas.Width, canvas.Height, 0, 0, -1);
        var worldToCanvas = viewMatrix * projectionMatrix * Matrix.Invert(canvasToNdc);

        // 处理星球选择
        Entity? pointedPlanet = null;
        HandleSelectionStateTransition(ref status.Selection, in worldToCanvas, in camera.Output,
                                       ofParty.Relationship!.Value.Copy.Party, ref pointedPlanet, commandBuffer);
        UpdateSelectionStatus(ref status.Selection, in worldToCanvas, in camera.Output,
                              ofParty.Relationship!.Value.Copy.Party, ref pointedPlanet);
    }

    public void Update(CommandBuffer commandBuffer) => HandleInputsQuery(world, commandBuffer);
}
