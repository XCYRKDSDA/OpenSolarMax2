using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string DependencyCapable = "DependencyCapable";
}

[Define(ConceptNames.DependencyCapable)]
public abstract class DependencyCapableDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(Dependence.AsDependency),
        typeof(Dependence.AsDependent)
    );
}

[Describe(ConceptNames.DependencyCapable)]
public record DependencyCapableDescription : IDescription
{
    public Entity[] Dependencies { get; set; } = [];

    public Entity[] Dependents { get; set; } = [];
}

[Apply(ConceptNames.DependencyCapable)]
public class DependencyCapableApplier(IConceptFactory factory) : IApplier<DependencyCapableDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, DependencyCapableDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        foreach (var dependency in desc.Dependencies)
        {
            _ = factory.Make(world, commandBuffer, new DependenceDescription()
            {
                Dependency = dependency,
                Dependent = entity,
            });
        }

        foreach (var dependent in desc.Dependents)
        {
            _ = factory.Make(world, commandBuffer, new DependenceDescription()
            {
                Dependency = entity,
                Dependent = dependent,
            });
        }
    }
}
