using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 没有任何特殊手势
/// </summary>
public struct InputGesture_Idle()
{
    /// <summary>
    /// 按下时鼠标的位置，空值表示没有按住。屏幕坐标系，左上角为原点
    /// </summary>
    public Point? PressedPosition = null;

    /// <summary>
    /// 已按住时长，仅在按下记录存在时有意义
    /// </summary>
    public TimeSpan HeldTime = default;
}

/// <summary>
/// 长按手势
/// </summary>
public struct InputGesture_Holding()
{
    /// <summary>
    /// 按下时鼠标的位置。屏幕坐标系，左上角为原点
    /// </summary>
    public Point PressedPosition = default;
}

/// <summary>
/// 拖拽手势
/// </summary>
public struct InputGesture_Dragging()
{
    /// <summary>
    /// 拖拽开始时鼠标按下的位置。屏幕坐标系，左上角为原点
    /// </summary>
    public Point PressedPosition = default;

    /// <summary>
    /// 鼠标当前的位置。屏幕坐标系，左上角为原点
    /// </summary>
    public Point CurrentPosition = default;
}

/// <summary>
/// 输入手势组件。记录由鼠标、触摸或者任何输入设备形成的复杂动作
/// </summary>
[Component]
public struct InputGesture()
{
    /// <summary>
    /// 当前输入手势
    /// </summary>
    public OneOf<InputGesture_Idle, InputGesture_Holding, InputGesture_Dragging> State =
        new InputGesture_Idle();
}
