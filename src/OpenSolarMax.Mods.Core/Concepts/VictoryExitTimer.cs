using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string VictoryExitTimer = "VictoryExitTimer";
}

[Define(ConceptNames.VictoryExitTimer)]
public abstract class VictoryExitTimerDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(VictoryExitTimer));
}

[Describe(ConceptNames.VictoryExitTimer)]
public class VictoryExitTimerDescription : IDescription
{
    public required TimeSpan TimeLeft { get; set; }
}

[Apply(ConceptNames.VictoryExitTimer)]
public class VictoryExitTimerApplier : IApplier<VictoryExitTimerDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, VictoryExitTimerDescription desc)
    {
        commandBuffer.Set(in entity, new VictoryExitTimer { TimeLeft = desc.TimeLeft });
    }
}
