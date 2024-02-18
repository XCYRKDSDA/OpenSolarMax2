using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 同步轨道组件。描述某个星球的同步轨道的姿态、半径和转速
/// </summary>
public struct PlanetGeostationaryOrbit
{
    /// <summary>
    /// 轨道姿态。描述轨道圆所在坐标系相对星球坐标系的旋转
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// 轨道半径
    /// </summary>
    public float Radius;

    /// <summary>
    /// 公转周期
    /// </summary>
    public float Period;
};
