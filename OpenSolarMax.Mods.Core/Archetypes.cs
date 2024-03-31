using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core;

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
        typeof(PlanetGeostationaryOrbit),
        typeof(Tree<Party>.Child),
        typeof(Tree<Anchorage>.Parent),
        typeof(AnchoredShipsRegistry),
        typeof(ProductionAbility),
        typeof(ProductionState),
        typeof(ReferenceSize),
        typeof(Battlefield),
        typeof(Animation)
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
        typeof(Tree<Anchorage>.Child),
        typeof(Animation)
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
        typeof(PartyReferenceColor),
        typeof(Producible),
        typeof(Combatable)
    );

    public static readonly Archetype View = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Camera),
        typeof(ManeuvaringShipsStatus)
    );

    public static readonly Archetype Animation = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(Animation),
        typeof(ExpiredAfterTimeout)
    );
}
