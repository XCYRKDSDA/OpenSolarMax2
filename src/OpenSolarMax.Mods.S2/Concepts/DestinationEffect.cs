using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DestinationEffect = "DestinationEffect";
}

[Define(ConceptNames.DestinationEffect)]
public abstract class DestinationEffectDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + new Signature(typeof(DestinationEffectAssignment), typeof(InTeam.AsAffiliate));
}

[Describe(ConceptNames.DestinationEffect)]
public class DestinationEffectDescription : IDescription
{
    public required Entity Warp { get; set; }

    public required float WarpRadius { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.DestinationEffect)]
public class DestinationEffectApplier(IConceptFactory factory)
    : IApplier<DestinationEffectDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, DestinationEffectDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = factory.Make(
            world,
            commandBuffer,
            ConceptNames.DestinationBackFlare,
            new DestinationBackFlareDescription
            {
                Effect = entity,
                Radius = desc.WarpRadius * 2f,
                Team = desc.Team,
            }
        );

        var surroundFlares = new List<Entity>();
        for (int i = 0; i < 3; i++)
        {
            surroundFlares.Add(
                factory.Make(
                    world,
                    commandBuffer,
                    ConceptNames.DestinationSurroundFlare,
                    new DestinationSurroundFlareDescription
                    {
                        Effect = entity,
                        Radius = desc.WarpRadius * 2f,
                        Team = desc.Team,
                        Angle = i * MathF.PI * 2 / 3,
                    }
                )
            );
        }

        // TODO：检查 Entity 引用情况
        commandBuffer.Set(
            in entity,
            new DestinationEffectAssignment(surroundFlares.ToArray(), backFlare)
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Warp }
        );
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription
            {
                Parent = desc.Warp,
                Child = entity,
                Translation = Vector3.Zero with { Z = 500 }, // 保证位于前边
            }
        );

        // 设置阵营
        if (desc.Team != Entity.Null)
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.InTeam,
                new InTeamDescription { Team = desc.Team, Affiliate = entity }
            );
    }
}
