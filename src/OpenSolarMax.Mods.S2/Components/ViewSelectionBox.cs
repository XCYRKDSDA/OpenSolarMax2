using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 视图与选框的对应关系
/// </summary>
[Relationship]
public readonly partial struct ViewSelectionBox(Entity view, Entity box)
{
    /// <summary>
    /// 视图实体。独占：一个视图只有一个选框。
    /// </summary>
    [Participant]
    public readonly Entity View = view;

    /// <summary>
    /// 选框实体。独占：一个选框只属于一个视图。
    /// </summary>
    [Participant]
    public readonly Entity Box = box;
}
