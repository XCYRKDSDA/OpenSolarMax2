﻿using OpenSolarMax.Game;
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
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(PlanetGeostationaryOrbit),
            typeof(InParty.AsAffiliate),
            typeof(TreeRelationship<Anchorage>.AsParent),
            typeof(AnchoredShipsRegistry),
            typeof(ProductionAbility),
            typeof(ReferenceSize),
            typeof(Battlefield),
            typeof(Animation),
            typeof(Colonizable)
        );

    public static readonly Archetype Ship =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(RevolutionOrbit),
            typeof(RevolutionState),
            typeof(InParty.AsAffiliate),
            typeof(TreeRelationship<Anchorage>.AsChild),
            typeof(Animation),
            typeof(TrailOf.AsShip),
            typeof(PopulationCost),
            typeof(SoundEffect)
        );

    public static readonly Archetype PredefinedOrbit =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
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
            typeof(InParty.AsParty),
            typeof(PartyPopulationRegistry),
            typeof(ColonizationAbility)
        );

    public static readonly Archetype View =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(Camera),
            typeof(ManeuvaringShipsStatus),
            typeof(InParty.AsAffiliate),
            typeof(LevelUIContext),
            typeof(FMOD.Studio.System)
        );

    public static readonly Archetype Animation =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(Animation)
        );

    public static readonly Archetype CountDownAnimation =
        Dependentable + new Archetype(
            typeof(AbsoluteTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}
