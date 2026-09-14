namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 殖民速度组件。
/// 拥有该实体的实体能够对其所锚定的实体进行殖民。
/// 组件字段描述了该实体关于殖民其他实体的各项能力
/// </summary>
public struct ColonizationAbility
{
    /// <summary>
    /// 每秒殖民的进度
    /// </summary>
    public float ProgressPerSecond;
}
