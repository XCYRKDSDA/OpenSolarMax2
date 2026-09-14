using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 星球与选择圈的一对一关系。一个星球可以有多个选择圈（多视图场景），一个选择圈只属于一个星球。
/// </summary>
[Relationship]
public readonly partial struct PlanetSelectionRing(Entity planet, Entity ring)
{
    /// <summary>
    /// 星球实体。非独占：一个星球可以被多个视图选中，因此可以有多个选择圈。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 选择圈实体。独占：一个选择圈只属于一个星球。
    /// </summary>
    [Participant]
    public readonly Entity Ring = ring;
}
