using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 视图与选择圈的一对一关系。一个视图可以有多个选择圈（选中多个星球），一个选择圈只属于一个视图。
/// </summary>
[Relationship]
public readonly partial struct ViewSelectionRing(Entity view, Entity ring)
{
    /// <summary>
    /// 视图实体。非独占：一个视图可以选中多个星球，因此可以有多个选择圈。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity View = view;

    /// <summary>
    /// 选择圈实体。独占：一个选择圈只属于一个视图。
    /// </summary>
    [Participant]
    public readonly Entity Ring = ring;
}
