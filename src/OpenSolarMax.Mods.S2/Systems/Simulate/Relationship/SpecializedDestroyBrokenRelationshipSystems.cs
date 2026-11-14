using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Reactive]
public sealed class DestroyBrokenAnchorageRelationshipSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(registry) { }

[SimulateSystem, Reactive]
public sealed class DestroyBrokenTrailRelationshipSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<TrailOf>(registry) { }

/// <summary>
/// 清理已损坏的星球-选择圈关系。当星球或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetSelectionRingsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetSelectionRing>(registry) { }

/// <summary>
/// 清理已损坏的星球-候选选择圈关系。当星球或候选圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetCandidateRingsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetCandidateRing>(registry) { }

/// <summary>
/// 清理已损坏的视图-选框关系。当视图或选框被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenViewSelectionBoxesSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<ViewSelectionBox>(registry) { }

/// <summary>
/// 清理已损坏的星球-拖拽线段关系。当星球或线段被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetManeuveringLineSourcesSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetManeuveringLineSource>(registry) { }

/// <summary>
/// 清理已损坏的目标星球-拖拽线段关系。当星球或线段被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetManeuveringLineTargetsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetManeuveringLineTarget>(registry) { }

/// <summary>
/// 清理已损坏的世界-屏幕投影关系。当世界实体或屏幕实体被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenWorldToScreenProjectionsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<WorldToScreenProjection>(registry) { }

/// <summary>
/// 清理已损坏的首府关系。当首府天体或阵营被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, Reactive]
public sealed class DestroyBrokenCapitalOfSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<CapitalOf>(registry) { }

/// <summary>
/// 清理已损坏的运输请求。当出发或目的天体被销毁时，自动销毁请求实体。
/// </summary>
[SimulateSystem, Reactive]
public sealed class DestroyBrokenJumpingRequestSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<StartJumpingRequest>(registry) { }

/// <summary>
/// 清理已损坏的被点选实体-悬停发光关系。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPointedEntityHoverGlowsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PointedEntityHoverGlow>(registry) { }

/// <summary>
/// 清理已损坏的天体-殖民环关系。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetColonizationRingsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetColonizationRing>(registry) { }

/// <summary>
/// 清理已损坏的天体-舰船构成弧关系。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetAnchoredShipsArcsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetAnchoredShipsArc>(registry) { }

/// <summary>
/// 清理已损坏的天体-舰船数量标签关系。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class DestroyBrokenPlanetAnchoredShipsLabelsSystem(EventRegistry registry)
    : DestroyBrokenRelationshipsSystem<PlanetAnchoredShipsLabel>(registry) { }
