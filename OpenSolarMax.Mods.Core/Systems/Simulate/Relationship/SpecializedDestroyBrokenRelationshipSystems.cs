using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Reactive]
public sealed class DestroyBrokenTeamRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InTeam>(world) { }

[SimulateSystem, Reactive]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world) { }

[SimulateSystem, Reactive]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world) { }

[SimulateSystem, Reactive]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world) { }

/// <summary>
/// 清理已损坏的 ColorSync 关系。当参与方（父/子）被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, Reactive]
public sealed class DestroyBrokenColorSyncRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<ColorSync>>(world) { }

/// <summary>
/// 清理已损坏的星球-选择圈关系。当星球或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, Reactive]
public sealed class DestroyBrokenPlanetSelectionRingsSystem(World world)
    : DestroyBrokenRelationshipsSystem<PlanetSelectionRing>(world) { }

/// <summary>
/// 清理已损坏的视图-选择圈关系。当视图或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, Reactive]
public sealed class DestroyBrokenViewSelectionRingsSystem(World world)
    : DestroyBrokenRelationshipsSystem<ViewSelectionRing>(world) { }
