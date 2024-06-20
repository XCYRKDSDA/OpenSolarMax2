using OpenSolarMax.Game;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core;

public static class Archetypes
{
    public static readonly Archetype Transformable = new(
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );

    public static readonly Archetype Dependentable = new(
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency)
    );

    public static readonly Archetype Planet =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(RelativeTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(PlanetGeostationaryOrbit),
            typeof(TreeRelationship<Party>.AsChild),
            typeof(TreeRelationship<Anchorage>.AsParent),
            typeof(AnchoredShipsRegistry),
            typeof(ProductionAbility),
            typeof(ReferenceSize),
            typeof(Battlefield),
            typeof(Animation),
            typeof(Colonizable),
            typeof(ColonizationState) //一般而言所有星球都处在或多或少被殖民的状态，因此一直保留该组件
        );

    public static readonly Archetype Ship =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(RelativeTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(RevolutionOrbit),
            typeof(RevolutionState),
            typeof(TreeRelationship<Party>.AsChild),
            typeof(TreeRelationship<Anchorage>.AsChild),
            typeof(Animation),
            typeof(TrailOf.AsShip),
            typeof(PopulationCost)
        );

    public static readonly Archetype PredefinedOrbit =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(RelativeTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(PredefinedOrbit)
        );

    public static readonly Archetype Party =
        Dependentable + new Archetype(
            typeof(PartyReferenceColor),
            typeof(Producible),
            typeof(Combatable),
            typeof(Shippable),
            typeof(TreeRelationship<Party>.AsParent),
            typeof(PartyPopulationRegistry),
            typeof(ColonizationAbility)
        );

    public static readonly Archetype View =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(RelativeTransform),
            typeof(Camera),
            typeof(ManeuvaringShipsStatus),
            typeof(TreeRelationship<Party>.AsChild),
            typeof(LevelUIContext)
        );

    public static readonly Archetype Animation =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(RelativeTransform),
            typeof(TreeRelationship<Party>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(Animation),
            typeof(ExpiredAfterTimeout)
        );
}
