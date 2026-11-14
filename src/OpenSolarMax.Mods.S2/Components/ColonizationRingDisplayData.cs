using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体殖民进度环的显示数据。
/// 这些数据由各视图共用，且与屏幕空间无关，因此在仿真阶段算好
/// </summary>
[Component]
public struct ColonizationRingDisplayData
{
    /// <summary>
    /// 进度环的颜色，取自正在殖民的阵营的代表色
    /// </summary>
    public Color Color;

    /// <summary>
    /// 进度环的张角，为整周按殖民进度占体量的比例折算
    /// </summary>
    public float Angle;
}
