using OpenSolarMax.Core.Components;
using OpenSolarMax.Core.Utils;

namespace OpenSolarMax.Core;

public static class Archetypes
{
    public static readonly Archetype Transformable = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<RelativeTransform>)
    );

    public static readonly Archetype Planet = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<RelativeTransform>),
        typeof(Sprite),
        typeof(RevolutionOrbit),
        typeof(RevolutionState),
        typeof(PlanetGeostationaryOrbit)
    );

    public static readonly Archetype Ship = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<RelativeTransform>),
        typeof(Sprite),
        typeof(RevolutionOrbit),
        typeof(RevolutionState)
    );

    public static readonly Archetype Camera = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<RelativeTransform>),
        typeof(Camera)
    );
}
