using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string DestinationEffect = "DestinationEffect";
}

[Define(ConceptNames.DestinationEffect)]
public abstract class DestinationEffectDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature +
        TransformableDefinition.Signature +
        new Signature(
            typeof(DestinationEffectAssignment)
        );
}

[Describe(ConceptNames.DestinationEffect)]
public class DestinationEffectDescription : IDescription
{
    public required Entity Portal { get; set; }

    public required float PortalRadius { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.DestinationEffect)]
public class DestinationEffectApplier(IConceptFactory factory)
    : IApplier<DestinationEffectDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, DestinationEffectDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = factory.Make(world, commandBuffer, ConceptNames.DestinationBackFlare,
                                     new DestinationBackFlareDescription
                                     {
                                         Effect = entity,
                                         Radius = desc.PortalRadius * 2f,
                                         Color = desc.Color
                                     });

        var surroundFlares = new List<Entity>();
        for (int i = 0; i < 3; i++)
        {
            surroundFlares.Add(factory.Make(world, commandBuffer, ConceptNames.DestinationSurroundFlare,
                                            new DestinationSurroundFlareDescription
                                            {
                                                Effect = entity,
                                                Radius = desc.PortalRadius * 2f,
                                                Color = desc.Color,
                                                Angle = i * MathF.PI * 2 / 3
                                            }));
        }

        // TODO：检查 Entity 引用情况
        commandBuffer.Set(in entity, new DestinationEffectAssignment(surroundFlares.ToArray(), backFlare));

        factory.Make(world, commandBuffer, ConceptNames.Dependence,
                     new DependenceDescription { Dependent = entity, Dependency = desc.Portal });
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription
                     {
                         Parent = desc.Portal,
                         Child = entity,
                         Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
                     });
    }
}
