﻿using OpenSolarMax.Game.Utils;
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
        typeof(PlanetGeostationaryOrbit)
    );

    public static readonly Archetype Ship = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Sprite),
        typeof(RevolutionOrbit),
        typeof(RevolutionState)
    );

    public static readonly Archetype PredefinedOrbit = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(PredefinedOrbit)
    );

    public static readonly Archetype Camera = new(
        typeof(AbsoluteTransform),
        typeof(RelativeTransform),
        typeof(Tree<RelativeTransform>.Child),
        typeof(Tree<RelativeTransform>.Parent),
        typeof(Camera)
    );
}
