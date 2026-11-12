using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Concepts;
using OpenSolarMax.Mods.Common.Graphics;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string AnchoredShipsArc = "AnchoredShipsArc";
}

/// <summary>
/// 舰船构成弧实体定义
/// </summary>
[Define(ConceptNames.AnchoredShipsArc)]
public abstract class AnchoredShipsArcDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(Shape),
            typeof(ScreenTransform),
            typeof(WorldToScreenProjection.AsTarget),
            typeof(PlanetAnchoredShipsArc.AsArc),
            typeof(Dependence.AsDependent)
        );
}

[Describe(ConceptNames.AnchoredShipsArc)]
public class AnchoredShipsArcDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Team { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.AnchoredShipsArc)]
public class AnchoredShipsArcApplier(IConceptFactory factory)
    : IApplier<AnchoredShipsArcDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, AnchoredShipsArcDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new Shape { Geometry = new Shape.Arc(Vector2.Zero, 0, 0, 0, 0) }
        );
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
            ConceptNames.PlanetAnchoredShipsArc,
            new PlanetAnchoredShipsArcDescription
            {
                Planet = desc.Planet,
                Arc = entity,
                Team = desc.Team,
            }
        );
    }
}
