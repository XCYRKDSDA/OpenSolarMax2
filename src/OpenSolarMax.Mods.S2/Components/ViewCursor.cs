using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 视图与其光标实体的对应关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct ViewCursor(Entity view, Entity cursor)
{
    /// <summary>
    /// 所属视图。独占：一个视图只有一个光标
    /// </summary>
    [Participant]
    public readonly Entity View = view;

    /// <summary>
    /// 光标实体。独占：一个光标只属于一个视图
    /// </summary>
    [Participant]
    public readonly Entity Cursor = cursor;
}
