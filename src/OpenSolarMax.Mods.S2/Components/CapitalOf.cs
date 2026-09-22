using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体与阵营的一对一关系：该天体是该阵营的首府。
/// 两端独占——一个阵营只有一个首府，一个天体只能是一个阵营的首府；
/// 重复声明时独占索引在添加第二条关系时抛出异常，即关卡加载期失败。
/// </summary>
[Relationship]
public readonly partial struct CapitalOf(Entity capital, Entity team)
{
    /// <summary>
    /// 首府天体实体。独占：一个天体只能是一个阵营的首府
    /// </summary>
    [Participant]
    public readonly Entity Capital = capital;

    /// <summary>
    /// 首府所属的阵营实体。独占：一个阵营只有一个首府
    /// </summary>
    [Participant]
    public readonly Entity Team = team;
}
