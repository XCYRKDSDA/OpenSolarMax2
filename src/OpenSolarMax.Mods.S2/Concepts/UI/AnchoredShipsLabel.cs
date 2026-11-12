using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Concepts;
using OpenSolarMax.Mods.Common.Graphics;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string AnchoredShipsLabel = "AnchoredShipsLabel";
}

/// <summary>
/// 舰船数量标签实体定义
/// </summary>
[Define(ConceptNames.AnchoredShipsLabel)]
public abstract class AnchoredShipsLabelDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(Shape),
            typeof(ScreenTransform),
            typeof(WorldToScreenProjection.AsTarget),
            typeof(PlanetAnchoredShipsLabel.AsLabel),
            typeof(Dependence.AsDependent)
        );
}

[Describe(ConceptNames.AnchoredShipsLabel)]
public class AnchoredShipsLabelDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Team { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.AnchoredShipsLabel)]
public class AnchoredShipsLabelApplier(IConceptFactory factory)
    : IApplier<AnchoredShipsLabelDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        AnchoredShipsLabelDescription desc
    )
    {
        commandBuffer.Set(in entity, new Shape());
        commandBuffer.Set(in entity, new ScreenTransform { Z = (int)GraphicsLayer.Interface - 2 });

        var world = World.Worlds[entity.WorldId];

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.WorldToScreenProjection,
            new WorldToScreenProjectionDescription
            {
                Source = desc.Planet,
                Target = entity,
                View = desc.View,
                ProjectRotation = false,
            }
        );

        factory.Make(
            world,
            commandBuffer,
            new DependenceDescription { Dependent = entity, Dependency = desc.Planet }
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PlanetAnchoredShipsLabel,
            new PlanetAnchoredShipsLabelDescription
            {
                Planet = desc.Planet,
                Label = entity,
                Team = desc.Team,
            }
        );
    }
}
