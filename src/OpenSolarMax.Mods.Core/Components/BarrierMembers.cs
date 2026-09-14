using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public readonly struct BarrierMembers(Entity node1, Entity node2, Entity[] edgeParts)
{
    public readonly Entity Node1 = node1;

    public readonly Entity Node2 = node2;

    public readonly Entity[] EdgeParts = edgeParts;
}
