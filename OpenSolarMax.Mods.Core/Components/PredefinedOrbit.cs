using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 预定义的轨道。
/// 用于将一个实体定义为轨道，将星球加入到该轨道上即可采用预定义的轨道参数
/// </summary>
[Component]
public struct PredefinedOrbit()
{
    /// <summary>
    /// 预定义的轨道模板
    /// </summary>
    public RevolutionOrbit Template = new();
}
