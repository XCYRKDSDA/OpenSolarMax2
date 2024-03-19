using Arch.Core;
using OpenSolarMax.Game.System;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public struct AnchoredShipsRegistry
{
    public Lookup<Entity, Entity> Ships;
}
