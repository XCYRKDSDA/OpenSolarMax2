using System.Runtime.InteropServices;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

public enum ShipsSelection_State
{
    SimpleSelecting,
    BoxSelectingSources,
    DraggingToDestination,
}

[StructLayout(LayoutKind.Explicit)]
public struct ShipsSelection_SimpleSelecting()
{
    [FieldOffset(0)]
    public Entity PointingPlanet = Entity.Null;

    [FieldOffset(8)]
    public Entity TappingSource = Entity.Null;

    [FieldOffset(16)]
    public Entity TappingDestination = Entity.Null;

    [FieldOffset(32)]
    public HashSet<Entity> SelectedSources;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShipsSelection_BoxSelectingSources
{
    [FieldOffset(0)]
    public Point BoxStartInViewport;

    [FieldOffset(8)]
    public Rectangle BoxInViewport;

    [FieldOffset(32)]
    public HashSet<Entity> OtherSelectedPlanets;

    [FieldOffset(40)]
    public HashSet<Entity> PlanetsInBox;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShipsSelection_DraggingToDestination()
{
    [FieldOffset(0)]
    public Entity CandidateDestination = Entity.Null;

    [FieldOffset(32)]
    public HashSet<Entity> SelectedSources;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShipsSelection()
{
    [FieldOffset(0)]
    public ShipsSelection_State State;

    [FieldOffset(8)]
    public ShipsSelection_SimpleSelecting SimpleSelecting = new() { SelectedSources = [] };

    [FieldOffset(8)]
    public ShipsSelection_BoxSelectingSources BoxSelectingSources;

    [FieldOffset(8)]
    public ShipsSelection_DraggingToDestination DraggingToDestination;
}

/// <summary>
/// 操作单位的状态。描述某个视图实体上对世界中单位的操作状态
/// </summary>
[Component]
public struct ManeuvaringShipsStatus()
{
    public float SelectionRatio;

    public ShipsSelection Selection = new();
}
