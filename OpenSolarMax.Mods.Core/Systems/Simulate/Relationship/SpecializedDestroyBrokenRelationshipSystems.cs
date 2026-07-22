using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, LateUpdate]
[ReadCurr(typeof(InTeam)), ChangeStructure]
public sealed class DestroyBrokenTeamRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InTeam>(world) { }

[SimulateSystem, LateUpdate]
[ReadCurr(typeof(TreeRelationship<Anchorage>)), ChangeStructure]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world) { }

[SimulateSystem, LateUpdate]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>)), ChangeStructure]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world) { }

[SimulateSystem, LateUpdate]
[ReadCurr(typeof(TrailOf)), ChangeStructure]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world) { }

/// <summary>
/// 清理已损坏的星球-选择圈关系。当星球或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, LateUpdate]
[ReadCurr(typeof(PlanetSelectionRing)), ChangeStructure]
public sealed class DestroyBrokenPlanetSelectionRingsSystem(World world)
    : DestroyBrokenRelationshipsSystem<PlanetSelectionRing>(world) { }

/// <summary>
/// 清理已损坏的视图-选择圈关系。当视图或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, LateUpdate]
[ReadCurr(typeof(ViewSelectionRing)), ChangeStructure]
public sealed class DestroyBrokenViewSelectionRingsSystem(World world)
    : DestroyBrokenRelationshipsSystem<ViewSelectionRing>(world) { }
