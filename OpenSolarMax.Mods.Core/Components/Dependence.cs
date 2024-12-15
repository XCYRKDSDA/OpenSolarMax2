using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 依赖关系。当被依赖的实体死亡时，依赖其的实体也会被销毁。该逻辑由<see cref="Systems.ManageDependenceSystem"/>实现。
/// </summary>
[Relationship]
public readonly partial struct Dependence(EntityReference dependent, EntityReference dependency)
{
    [Participant(exclusive: false)]
    public readonly EntityReference Dependent = dependent;

    [Participant(exclusive: false)]
    public readonly EntityReference Dependency = dependency;
}
