using Arch.Core;

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

/// <summary>
/// 殖民状态组件。拥有该组件表明该实体正在被殖民。
/// 组件字段描述了该实体被殖民的具体状况
/// </summary>
public struct ColonizationState
{
    /// <summary>
    /// 殖民阵营。描述当前是哪个阵营在尝试殖民该实体
    /// </summary>
    public EntityReference Party;

    /// <summary>
    /// 殖民度。描述当前正在尝试殖民该实体的阵营对该实体的殖民程度。为绝对值
    /// </summary>
    public float Progress;
}
