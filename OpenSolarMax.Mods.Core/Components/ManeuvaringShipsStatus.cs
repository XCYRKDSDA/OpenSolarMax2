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
    /// <summary>
    /// 当前不管有没有点击，总之鼠标所在的星球
    /// </summary>
    [FieldOffset(0)]
    public EntityReference PointingPlanet = EntityReference.Null;

    /// <summary>
    /// 当前正在点击的出发星球星球
    /// </summary>
    [FieldOffset(16)]
    public EntityReference TappingSource = EntityReference.Null;

    /// <summary>
    /// 当前正在点击的目标星球
    /// </summary>
    [FieldOffset(32)]
    public EntityReference TappingDestination = EntityReference.Null;

    /// <summary>
    /// 当前状态下积累的所有出发星球
    /// </summary>
    [FieldOffset(48)]
    public HashSet<EntityReference> SelectedSources;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShipsSelection_BoxSelectingSources
{
    /// <summary>
    /// 在 Viewport 坐标系下，选框的起始点
    /// </summary>
    [FieldOffset(0)]
    public Point BoxStartInViewport;

    /// <summary>
    /// 在 Viewport 坐标系下，选框当前的矩形
    /// </summary>
    [FieldOffset(8)]
    public Rectangle BoxInViewport;

    /// <summary>
    /// 在进入框选状态前已经选中了的星球
    /// </summary>
    [FieldOffset(48)]
    public HashSet<EntityReference> OtherSelectedPlanets;

    /// <summary>
    /// 选框中的星球
    /// </summary>
    [FieldOffset(64)]
    public HashSet<EntityReference> PlanetsInBox;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShipsSelection_DraggingToDestination()
{
    /// <summary>
    /// 当前拖拽到的目标星球
    /// </summary>
    [FieldOffset(0)]
    public EntityReference CandidateDestination = EntityReference.Null;

    /// <summary>
    /// 当前选中的出发星球
    /// </summary>
    [FieldOffset(48)]
    public HashSet<EntityReference> SelectedSources;
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
