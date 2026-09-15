using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems;

[SimulateSystem, Reactive]
public sealed class IndexDependenceSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<Dependence>(registry) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexTeamAffiliationSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<InTeam>(registry) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexTransformTreeSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(registry) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexColorSyncTreeSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<ColorSync>>(registry) { }
