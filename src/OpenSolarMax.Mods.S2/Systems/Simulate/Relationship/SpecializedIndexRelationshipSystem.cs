using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Reactive]
public sealed class IndexAnchorageSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(registry) { }

[SimulateSystem, Reactive]
public sealed class IndexTrailAffiliationSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TrailOf>(registry) { }

/// <summary>
/// 索引星球与选择圈的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetSelectionRingSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetSelectionRing>(registry) { }

/// <summary>
/// 索引星球与候选选择圈的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetCandidateRingSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetCandidateRing>(registry) { }

/// <summary>
/// 索引视图与选框的关系，维护 AsView 和 AsBox 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexViewSelectionBoxSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<ViewSelectionBox>(registry) { }

/// <summary>
/// 索引星球与拖拽线段的关系，维护 AsPlanet 和 AsLine 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetManeuveringLineSourceSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetManeuveringLineSource>(registry) { }

/// <summary>
/// 索引目标星球与拖拽线段的关系，维护 AsPlanet 和 AsLine 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetManeuveringLineTargetSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetManeuveringLineTarget>(registry) { }

/// <summary>
/// 索引世界实体与屏幕实体的投影关系，维护 AsSource、AsTarget 和 AsView 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexWorldToScreenProjectionSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<WorldToScreenProjection>(registry) { }

/// <summary>
/// 索引天体与阵营的首府关系，维护 AsCapital 和 AsTeam 索引组件。
/// 两端独占，重复声明首府时在此抛出异常。
/// </summary>
[SimulateSystem, Reactive]
public sealed class IndexCapitalOfSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<CapitalOf>(registry) { }

/// <summary>
/// 索引出发天体与运输请求的关系，维护 AsDeparture 和 AsDestination 索引组件。
/// </summary>
[SimulateSystem, Reactive]
public sealed class IndexJumpingRequestSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<StartJumpingRequest>(registry) { }

/// <summary>
/// 索引被点选实体与悬停发光的关系，维护 AsPointedEntity、AsGlow 和 AsView 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPointedEntityHoverGlowSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PointedEntityHoverGlow>(registry) { }

/// <summary>
/// 索引天体与殖民环的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetColonizationRingSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetColonizationRing>(registry) { }

/// <summary>
/// 索引天体与舰船构成弧的关系，维护 AsPlanet 和 AsArc 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetAnchoredShipsArcSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetAnchoredShipsArc>(registry) { }

/// <summary>
/// 索引天体与舰船数量标签的关系，维护 AsPlanet 和 AsLabel 索引组件。
/// </summary>
[SimulateSystem, RenderSystem, Reactive]
public sealed class IndexPlanetAnchoredShipsLabelSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetAnchoredShipsLabel>(registry) { }
