using Microsoft.Xna.Framework.Input;

namespace OpenSolarMax.Game.Modding.UI;

/// <summary>
/// 视图的键盘设备状态。应由游戏本体在每帧输入系统运行前填写，
/// 各系统读取本组件获取键盘状态，不直接访问输入设备
/// </summary>
public struct KeyboardDeviceState
{
    /// <summary>
    /// 键盘状态
    /// </summary>
    public KeyboardState State;

    /// <summary>
    /// 上一帧的键盘状态
    /// </summary>
    public KeyboardState PreviousState;
}
