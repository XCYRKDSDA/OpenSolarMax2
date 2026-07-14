using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PendingVictoryEffect = "PendingVictoryEffect";
}

[Define(ConceptNames.PendingVictoryEffect)]
public abstract class PendingVictoryEffectDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(typeof(PendingVictoryEffect), typeof(VictoryEffectTarget));
}

[Describe(ConceptNames.PendingVictoryEffect)]
public class PendingVictoryEffectDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Winner { get; set; }
    public required TimeSpan TimeLeft { get; set; }
}

[Apply(ConceptNames.PendingVictoryEffect)]
public class PendingVictoryEffectApplier : IApplier<PendingVictoryEffectDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        PendingVictoryEffectDescription desc
    )
    {
        commandBuffer.Set(in entity, new PendingVictoryEffect { TimeLeft = desc.TimeLeft });

        commandBuffer.Set(
            in entity,
            new VictoryEffectTarget { Planet = desc.Planet, Winner = desc.Winner }
        );
    }
}
