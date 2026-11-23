using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体上停靠舰船的构成弧与数目标签的显示数据。
/// 这些数据由各视图共用，且与屏幕空间无关，因此在仿真阶段算好
/// </summary>
[Component]
public struct AnchoredShipsDisplayData()
{
    /// <summary>
    /// 停靠舰船所属的阵营，按实体标识排序
    /// </summary>
    public Entity[] Parties = [];

    /// <summary>
    /// <see cref="Parties"/> 的集合形式，供快速判断某个阵营是否在其中
    /// </summary>
    public HashSet<Entity> PartiesSet = [];

    /// <summary>
    /// 各阵营的舰船数，与 <see cref="Parties"/> 一一对应
    /// </summary>
    public int[] Weights = [];

    /// <summary>
    /// 各阵营的代表色，与 <see cref="Parties"/> 一一对应
    /// </summary>
    public Color[] Colors = [];

    /// <summary>
    /// 各阵营的数目标签文字，与 <see cref="Parties"/> 一一对应
    /// </summary>
    public string[] Texts = [];

    /// <summary>
    /// 各段构成弧的交界角度序列，长度为阵营数加一
    /// </summary>
    public float[] ArcAngles = [];

    /// <summary>
    /// 开启了舰船数目显示的阵营
    /// </summary>
    public HashSet<Entity> LabelledParties = [];
}
