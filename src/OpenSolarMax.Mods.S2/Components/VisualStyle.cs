namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 实体的视觉类型。决定实体采用所属阵营推荐的哪一种混合模式
/// </summary>
public enum VisualStyle
{
    /// <summary>
    /// 实心外观。如天体本体
    /// </summary>
    Solid,

    /// <summary>
    /// 发光外观。如舰船、光晕、尾迹与各类特效
    /// </summary>
    Effect,
}
