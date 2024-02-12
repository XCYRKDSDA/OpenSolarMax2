using OpenSolarMax.Core.Components;
using OpenSolarMax.Core.Utils;

namespace OpenSolarMax.Core;

public static class Archetypes
{
    public static readonly Archetype Planet = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<RelativeTransform>),
        typeof(Sprite)
    );

    public static readonly Archetype Camera = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<RelativeTransform>),
        typeof(Camera)
    );
}
