using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Reactive]
public sealed class DestroyBrokenTeamRelationshipSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<InTeam>(registry) { }

[SimulateSystem, Reactive]
public sealed class DestroyBrokenTransformRelationshipSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(registry) { }

/// <summary>
/// 清理已损坏的 ColorSync 关系。当参与方（父/子）被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, Reactive]
public sealed class DestroyBrokenColorSyncRelationshipSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<ColorSync>>(registry) { }
