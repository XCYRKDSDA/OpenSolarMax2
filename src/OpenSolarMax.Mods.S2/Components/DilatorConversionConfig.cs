using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

[Component]
public struct DilatorConversionConfig
{
    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public Entity ConversionTeam;

    /// <summary>
    /// 转化波半径的扩张速度
    /// </summary>
    public float ConversionSpeed;

    /// <summary>
    /// 转化波释放后到退出关卡的延迟
    /// </summary>
    public TimeSpan ExitDelay;
}
