using Arch.Core;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core;

public static class Signatures
{
    public static readonly Signature Transformable = new(
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );

    public static readonly Signature Dependentable = new(
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency)
    );

    public static readonly Signature Planet =
        Dependentable + new Signature(
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

    public static readonly Signature Ship =
        Dependentable + new Signature(
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

    public static readonly Signature PredefinedOrbit =
        Dependentable + new Signature(
            typeof(AbsoluteTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(PredefinedOrbit)
        );

    public static readonly Signature Party =
        Dependentable + new Signature(
            typeof(PartyReferenceColor),
            typeof(Producible),
            typeof(Combatable),
            typeof(Shippable),
            typeof(InParty.AsParty),
            typeof(PartyPopulationRegistry),
            typeof(ColonizationAbility)
        );

    public static readonly Signature View =
        Dependentable + new Signature(
            typeof(AbsoluteTransform),
            typeof(Camera),
            typeof(ManeuvaringShipsStatus),
            typeof(InParty.AsAffiliate),
            // typeof(LevelUIContext),
            typeof(FMOD.Studio.System)
        );

    public static readonly Signature Animation =
        Dependentable + new Signature(
            typeof(AbsoluteTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(Animation)
        );

    public static readonly Signature CountDownAnimation =
        Dependentable + new Signature(
            typeof(AbsoluteTransform),
            typeof(TreeRelationship<RelativeTransform>.AsChild),
            typeof(TreeRelationship<RelativeTransform>.AsParent),
            typeof(Sprite),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}
