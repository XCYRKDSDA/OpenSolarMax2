using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexDependenceSystem(World world)
    : IndexRelationshipSystemBase<Dependence>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexPartyAffiliationSystem(World world)
    : IndexRelationshipSystemBase<InParty>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexAnchorageSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTransformTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTrailAffiliationSystem(World world)
    : IndexRelationshipSystemBase<TrailOf>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexShootSystem(World world)
    : IndexRelationshipSystemBase<Shoot>(world)
{ }
