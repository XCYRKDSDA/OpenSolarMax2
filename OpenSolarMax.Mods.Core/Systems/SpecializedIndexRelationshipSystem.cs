using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(AnimateSystem))]
public sealed class IndexDependenceSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystem<Dependence>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(AnimateSystem))]
public sealed class IndexPartyAffiliationSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystem<TreeRelationship<Party>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(AnimateSystem))]
public sealed class IndexAnchorageSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystem<TreeRelationship<Anchorage>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(AnimateSystem))]
public sealed class IndexTransformTreeSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystem<TreeRelationship<RelativeTransform>>(world)
{ }

[LateUpdateSystem]
[ExecuteAfter(typeof(AnimateSystem))]
public sealed class IndexTrailAffiliationSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystem<TrailOf>(world)
{ }
