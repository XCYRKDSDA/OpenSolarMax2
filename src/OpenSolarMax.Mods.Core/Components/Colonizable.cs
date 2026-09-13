namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 殖民组件。
/// 拥有该组件表明该实体能够被殖民
/// </summary>
public struct Colonizable
{
    /// <summary>
    /// 该实体的体量。殖民进度必须超过该值才算殖民成功
    /// </summary>
    public float Volume;
}
