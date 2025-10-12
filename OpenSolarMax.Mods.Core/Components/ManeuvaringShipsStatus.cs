using System.Runtime.InteropServices;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Mods.Core.Components;

public enum ShipsSelection_State
{
    SimpleSelecting,
    BoxSelectingSources,
    DraggingToDestination,
}

public struct ShipsSelection_SimpleSelecting()
{
    /// <summary>
    /// 当前不管有没有点击，总之鼠标所在的星球
    /// </summary>
    public Entity PointingPlanet = Entity.Null;

    /// <summary>
    /// 当前正在点击的出发星球星球
    /// </summary>
    public Entity TappingSource = Entity.Null;

    /// <summary>
    /// 当前正在点击的目标星球
    /// </summary>
    public Entity TappingDestination = Entity.Null;

    /// <summary>
    /// 当前状态下积累的所有出发星球
    /// </summary>
    public HashSet<Entity> SelectedSources;
}

public struct ShipsSelection_BoxSelectingSources
{
    /// <summary>
    /// 在 Viewport 坐标系下，选框的起始点
    /// </summary>
    public Point BoxStartInViewport;

    /// <summary>
    /// 在 Viewport 坐标系下，选框当前的矩形
    /// </summary>
    public Rectangle BoxInViewport;

    /// <summary>
    /// 在进入框选状态前已经选中了的星球
    /// </summary>
    public HashSet<Entity> OtherSelectedPlanets;

    /// <summary>
    /// 选框中的星球
    /// </summary>
    public HashSet<Entity> PlanetsInBox;
}

public struct ShipsSelection_DraggingToDestination()
{
    /// <summary>
    /// 当前拖拽到的目标星球
    /// </summary>
    public Entity CandidateDestination = Entity.Null;

    /// <summary>
    /// 当前选中的出发星球
    /// </summary>
    public HashSet<Entity> SelectedSources;
}

public struct ShipsSelection()
{
    public ShipsSelection_State State;

    public ShipsSelection_SimpleSelecting SimpleSelecting = new() { SelectedSources = [] };

    public ShipsSelection_BoxSelectingSources BoxSelectingSources;

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
