using Microsoft.Xna.Framework.Input;

namespace OpenSolarMax.Game.Modding.UI;

/// <summary>
/// 视图的鼠标设备状态。应由本体在每帧输入系统运行前填写，
/// 各系统读取本组件获取鼠标状态，不直接访问输入设备。
/// </summary>
public struct MouseDeviceState
{
    /// <summary>
    /// 鼠标状态。其坐标位于窗口坐标系，左上角为原点
    /// </summary>
    public MouseState State;

    /// <summary>
    /// 上一帧的鼠标状态
    /// </summary>
    public MouseState PreviousState;
}
