using Arch.Core;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public readonly struct Battlefield()
{
    public readonly Dictionary<Entity, float> FrontlineDamage = [];
}
