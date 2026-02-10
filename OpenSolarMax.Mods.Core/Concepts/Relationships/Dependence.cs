using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Dependence = "Dependence";
}

[Define(ConceptNames.Dependence)]
public abstract class DependenceDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(Dependence)
    );
}

[Describe(ConceptNames.Dependence)]
public class DependenceDescription : IDescription
{
    public required Entity Dependent { get; set; }

    public required Entity Dependency { get; set; }
}

[Apply(ConceptNames.Dependence)]
public class DependenceApplier : IApplier<DependenceDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, DependenceDescription desc)
    {
        commandBuffer.Set(in entity, new Dependence(desc.Dependent, desc.Dependency));
    }
}
