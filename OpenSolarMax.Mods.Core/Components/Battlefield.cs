using Arch.Core;
using OpenSolarMax.Game.System;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public readonly struct Battlefield()
{
    public readonly Dictionary<Entity, float> FrontlineDamage = [];
}
