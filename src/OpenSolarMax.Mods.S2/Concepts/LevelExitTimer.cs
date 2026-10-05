using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string LevelExitTimer = "LevelExitTimer";
}

[Define(ConceptNames.LevelExitTimer)]
public abstract class LevelExitTimerDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(LevelExitTimer));
}

[Describe(ConceptNames.LevelExitTimer)]
public class LevelExitTimerDescription : IDescription
{
    public required TimeSpan TimeLeft { get; set; }
}

[Apply(ConceptNames.LevelExitTimer)]
public class LevelExitTimerApplier : IApplier<LevelExitTimerDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, LevelExitTimerDescription desc)
    {
        commandBuffer.Set(in entity, new LevelExitTimer { TimeLeft = desc.TimeLeft });
    }
}
