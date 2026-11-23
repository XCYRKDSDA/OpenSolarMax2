using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

internal static class PredefinedOrbitReference
{
    public static RevolutionOptions ToRevolutionOptions(
        string orbit,
        float? phase,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var orbitEntity = otherEntities[orbit];
        ref readonly var predefined = ref orbitEntity.Get<PredefinedOrbit>();

        return new RevolutionOptions
        {
            Parent = orbitEntity,
            Shape = predefined.Template.Shape,
            Period = predefined.Template.Period,
            Rotation = predefined.Template.Rotation,
            InitPhase = (phase ?? 0) * MathF.PI * 2,
        };
    }
}
