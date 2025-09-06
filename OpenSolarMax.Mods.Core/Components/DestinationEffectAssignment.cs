using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public readonly struct DestinationEffectAssignment(Entity[] surroundFlares, Entity backFlare)
{
    public readonly Entity[] SurroundFlares = surroundFlares;

    public readonly Entity BackFlare = backFlare;
}
