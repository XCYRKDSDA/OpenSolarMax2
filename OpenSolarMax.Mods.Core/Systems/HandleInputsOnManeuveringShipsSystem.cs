using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[StructuralChangeSystem]
[ExecuteAfter(typeof(SettleProductionSystem))]
[ExecuteAfter(typeof(SettleCombatSystem))]
public sealed partial class HandleInputsOnManeuveringShipsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const int _minimalSelectPixels = 10;

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
    private static void CheckPointedPlanet(Entity planet, in AbsoluteTransform pose,
                                           [Data] in Point mouseInViewport, [Data] in Matrix worldToViewport,
                                           [Data] ref EntityReference pointedPlanet, [Data] ref float pointedPlanetZ)
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
            pointedPlanet = planet.Reference();
            pointedPlanetZ = positionInViewport.Z;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EntityReference GetPointedPlanet(in Point mouseInViewport, in Matrix worldToViewport)
    {
        EntityReference pointedPlanet = EntityReference.Null;
        float pointedPlanetZ = float.PositiveInfinity;
        CheckPointedPlanetQuery(World, in mouseInViewport, in worldToViewport, ref pointedPlanet, ref pointedPlanetZ);
        return pointedPlanet;
    }

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
    private static void CheckBoxedPlanets(Entity planet, in AbsoluteTransform pose,
                                          [Data] in Rectangle box, [Data] in Matrix worldToViewport,
                                          [Data] ref HashSet<EntityReference> boxedPlanets)
    {
        var positionInViewport = Vector3.Transform(pose.Translation, worldToViewport);
        if (box.Contains(positionInViewport.X, positionInViewport.Y))
            boxedPlanets.Add(planet.Reference());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HashSet<EntityReference> GetBoxedPlanets(in Rectangle box, in Matrix worldToViewport)
    {
        var boxedPlanets = new HashSet<EntityReference>();
        CheckBoxedPlanetsQuery(World, in box, in worldToViewport, ref boxedPlanets);
        return boxedPlanets;
    }

    private void HandleSelectionStateTransition(ref ShipsSelection selection,
                                                in Matrix worldToViewport, in Viewport viewport, EntityReference party,
                                                ref EntityReference? pointedPlanet)
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();

        var mouseInViewport = new Point(mouse.X - viewport.X, mouse.Y - viewport.Y);

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
                    if (departure == selection.SimpleSelecting.TappingDestination)
                        continue;

                    ServiceUtils.Call(World, new StartShippingRequest()
                    {
                        Departure = departure,
                        Destination = selection.SimpleSelecting.TappingDestination,
                        Party = party,
                        ExpectedNum = departure.Entity.Get<AnchoredShipsRegistry>().Ships[party].Count()
                    });
                }
                selection.State = ShipsSelection_State.SimpleSelecting;
                selection.SimpleSelecting = new() { SelectedSources = [] };
            }
        }
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
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            if (mouse.LeftButton == ButtonState.Released)
            {
                selection.State = ShipsSelection_State.SimpleSelecting;

                if (selection.DraggingToDestination.CandidateDestination != Entity.Null)
                {
                    foreach (var departure in selection.DraggingToDestination.SelectedSources)
                    {
                        if (departure == selection.DraggingToDestination.CandidateDestination)
                            continue;

                        ServiceUtils.Call(World, new StartShippingRequest()
                        {
                            Departure = departure,
                            Destination = selection.DraggingToDestination.CandidateDestination,
                            Party = party,
                            ExpectedNum = departure.Entity.Get<AnchoredShipsRegistry>().Ships[party].Count()
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
                                       in Matrix worldToViewport, in Viewport viewport, EntityReference party,
                                       ref EntityReference? pointedPlanet)
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();

        var mouseInViewport = new Point(mouse.X - viewport.X, mouse.Y - viewport.Y);

        if (selection.State == ShipsSelection_State.SimpleSelecting)
        {
            selection.SimpleSelecting.PointingPlanet =
                pointedPlanet ??= GetPointedPlanet(in mouseInViewport, worldToViewport);

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var tappingSource = selection.SimpleSelecting.PointingPlanet;
                selection.SimpleSelecting.TappingSource = selection.SimpleSelecting.PointingPlanet;
                if (tappingSource != EntityReference.Null)
                {
                    if (keys[Keys.LeftShift] != KeyState.Down)
                        selection.SimpleSelecting.SelectedSources.Clear();
                    selection.SimpleSelecting.SelectedSources.Add(tappingSource);
                }
            }
            else
                selection.SimpleSelecting.TappingSource = EntityReference.Null;

            if (mouse.RightButton == ButtonState.Pressed)
            {
                // 将当前右键点选的对象，不论有没有点选在星球上，都进行记录
                var tappingDestination = selection.SimpleSelecting.PointingPlanet;
                selection.SimpleSelecting.TappingDestination = tappingDestination;
            }
            else
                selection.SimpleSelecting.TappingDestination = EntityReference.Null;
        }
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
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            // 在拖拽状态下时，计算并记录当前拖拽到的目标星球
            var hoveringDestination = pointedPlanet ??= GetPointedPlanet(in mouseInViewport, worldToViewport);
            selection.DraggingToDestination.CandidateDestination = hoveringDestination;
        }
    }

    [Query]
    [All<Camera, AbsoluteTransform, ManeuvaringShipsStatus, TreeRelationship<Party>.AsChild>]
    private void HandleInputs(in Camera camera, in AbsoluteTransform pose, ref ManeuvaringShipsStatus status,
                              in TreeRelationship<Party>.AsChild ofParty)
    {
        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var canvas = camera.Output.Bounds;
        var canvasToNdc = Matrix.CreateOrthographicOffCenter(0, canvas.Width, canvas.Height, 0, 0, -1);
        var worldToCanvas = viewMatrix * projectionMatrix * Matrix.Invert(canvasToNdc);

        // 处理星球选择
        EntityReference? pointedPlanet = null;
        HandleSelectionStateTransition(ref status.Selection, in worldToCanvas, in camera.Output, ofParty.Index.Parent,
                                       ref pointedPlanet);
        UpdateSelectionStatus(ref status.Selection, in worldToCanvas, in camera.Output, ofParty.Index.Parent,
                              ref pointedPlanet);
    }

    public override void Update(in GameTime data) => HandleInputsQuery(World);
}
