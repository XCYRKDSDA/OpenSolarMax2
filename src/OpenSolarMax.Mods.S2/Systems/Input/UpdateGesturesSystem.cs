using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using InputGestureState = OneOf.OneOf<
    OpenSolarMax.Mods.S2.Components.InputGesture_Idle,
    OpenSolarMax.Mods.S2.Components.InputGesture_Holding,
    OpenSolarMax.Mods.S2.Components.InputGesture_Dragging
>;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 手势系统。监控视图输入，识别特殊手势，写入 <see cref="InputGesture"/>
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(MouseDeviceState))]
[Calc(typeof(InputGesture))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(InputGesture))]
public sealed partial class UpdateGesturesSystem(
    World world,
    [Section("systems:input")] IConfiguration configs
) : ICalcSystem
{
    /// <summary>
    /// 拖拽防抖阈值（像素）。按住期间位移的切比雪夫距离超过该值才进入拖拽
    /// </summary>
    private readonly int _draggingThreshold = configs.RequireValue<int>(
        "dragging_threshold_pixels"
    );

    /// <summary>
    /// 长按防抖阈值。已按住时长达到该值才进入长按
    /// </summary>
    private readonly TimeSpan _holdingThreshold = TimeSpan.FromSeconds(
        configs.RequireValue<double>("holding_threshold_seconds")
    );

    /// <summary>
    /// 位移的切比雪夫距离是否超过拖拽防抖阈值
    /// </summary>
    private bool ExceedsThreshold(Point position, Point pressedPosition)
    {
        var delta = position - pressedPosition;
        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) > _draggingThreshold;
    }

    [Query]
    [All<MouseDeviceState, InputGesture>]
    private void UpdateGesture(in MouseDeviceState mouse, ref InputGesture gesture)
    {
        // in 参数不能被 lambda 捕获，先复制到局部变量
        var mouseState = mouse.State;

        if (mouseState.LeftButton != ButtonState.Pressed)
        {
            gesture.State = new InputGesture_Idle();
            return;
        }

        gesture.State = gesture.State.Match<InputGestureState>(
            idle =>
                idle.PressedPosition is { } pressed
                    // 如果之前按下，则判断下一个状态
                    ? ExceedsThreshold(mouseState.Position, pressed)
                        // 如果移动量超过阈值，则为拖拽
                        ? new InputGesture_Dragging
                        {
                            PressedPosition = pressed,
                            CurrentPosition = mouseState.Position,
                        }
                        // 如果移动量不足但点下时间足够长，则为长按
                        : idle.HeldTime >= _holdingThreshold
                            ? new InputGesture_Holding { PressedPosition = pressed }
                            : idle
                    // 如果之前没有按下，则开始按下并开始计时
                    : new InputGesture_Idle
                    {
                        PressedPosition = mouseState.Position,
                        HeldTime = TimeSpan.Zero,
                    },
            holding =>
                // 即使位于长按状态，如果移动量超过阈值，也变为拖拽
                ExceedsThreshold(mouseState.Position, holding.PressedPosition)
                    ? new InputGesture_Dragging
                    {
                        PressedPosition = holding.PressedPosition,
                        CurrentPosition = mouseState.Position,
                    }
                    : holding,
            dragging => new InputGesture_Dragging
            {
                PressedPosition = dragging.PressedPosition,
                CurrentPosition = mouseState.Position,
            }
        );
    }

    public void Update() => UpdateGestureQuery(world);
}
