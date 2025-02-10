using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public enum ColonizationEvent
{
    Idle,
    Progressing,
    Destroying,
    Finished,
    Destroyed,
}

/// <summary>
/// 殖民状态组件。拥有该组件表明该实体正在被殖民。
/// 组件字段描述了该实体被殖民的具体状况
/// </summary>
public struct ColonizationState()
{
    /// <summary>
    /// 殖民阵营。描述当前是哪个阵营在尝试殖民该实体
    /// </summary>
    public EntityReference Party = EntityReference.Null;

    /// <summary>
    /// 殖民度。描述当前正在尝试殖民该实体的阵营对该实体的殖民程度。为绝对值
    /// </summary>
    public float Progress = 0;

    public ColonizationEvent Event = ColonizationEvent.Idle;
}
