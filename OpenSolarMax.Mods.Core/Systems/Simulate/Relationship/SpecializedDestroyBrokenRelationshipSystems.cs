// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(InTeam)), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTeamRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InTeam>(world) { }

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(TreeRelationship<Anchorage>)), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world) { }

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>)), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world) { }

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(TrailOf)), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world) { }

#endif
