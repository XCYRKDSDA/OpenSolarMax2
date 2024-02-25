using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Core;

public static class Archetypes
{
    public static readonly Archetype Transformable = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent)
    );

    public static readonly Archetype Planet = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(RevolutionOrbit),
        typeof(RevolutionState),
        typeof(PlanetGeostationaryOrbit),
        typeof(Tree<Party>.Child),
        typeof(Tree<Anchorage>.Parent),
        typeof(AnchoredShipsRegistry)
    );

    public static readonly Archetype Ship = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(RevolutionOrbit),
        typeof(RevolutionState),
        typeof(Tree<Party>.Child),
        typeof(Tree<Anchorage>.Child)
    );

    public static readonly Archetype PredefinedOrbit = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(PredefinedOrbit)
    );

    public static readonly Archetype Party = new(
        typeof(Tree<Party>.Parent),
        typeof(PartyReferenceColor)
    );

    public static readonly Archetype Camera = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Camera)
    );
}
