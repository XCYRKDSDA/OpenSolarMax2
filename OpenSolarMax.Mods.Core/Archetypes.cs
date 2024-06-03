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

    public static readonly Archetype Dependentable = new(
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency)
    );

    public static readonly Archetype Planet = Dependentable + new Archetype(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(PlanetGeostationaryOrbit),
        typeof(TreeRelationship<Party>.AsChild),
        typeof(Tree<Anchorage>.Parent),
        typeof(AnchoredShipsRegistry),
        typeof(ProductionAbility),
        typeof(ReferenceSize),
        typeof(Battlefield),
        typeof(Animation)
    );

    public static readonly Archetype Ship = Dependentable + new Archetype(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(RevolutionOrbit),
        typeof(RevolutionState),
        typeof(TreeRelationship<Party>.AsChild),
        typeof(Tree<Anchorage>.Child),
        typeof(Animation)
    );

    public static readonly Archetype PredefinedOrbit = Dependentable + new Archetype(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(PredefinedOrbit)
    );

    public static readonly Archetype Party = Dependentable + new Archetype(
        typeof(PartyReferenceColor),
        typeof(Producible),
        typeof(Combatable),
        typeof(Shippable),
        typeof(TreeRelationship<Party>.AsParent)
    );

    public static readonly Archetype View = Dependentable + new Archetype(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Camera),
        typeof(ManeuvaringShipsStatus),
        typeof(TreeRelationship<Party>.AsChild)
    );

    public static readonly Archetype Animation = Dependentable + new Archetype(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(TreeRelationship<Party>.AsChild),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(Animation),
        typeof(ExpiredAfterTimeout)
    );
}
