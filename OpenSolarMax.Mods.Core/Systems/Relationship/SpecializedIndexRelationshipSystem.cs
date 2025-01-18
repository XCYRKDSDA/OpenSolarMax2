using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexDependenceSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystemBase<Dependence>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexPartyAffiliationSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystemBase<InParty>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexAnchorageSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTransformTreeSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTrailAffiliationSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystemBase<TrailOf>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexPortalChargingEffectSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystemBase<InPortalEffect>(world)
{ }
