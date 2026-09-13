using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public struct PlanetAiTimers()
{
    public Dictionary<Entity, TimeSpan> TimeLeft = [];
}
