using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ManeuveringLine = "ManeuveringLine";
}

[Define(ConceptNames.ManeuveringLine)]
public abstract class ManeuveringLineDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(Shape),
            typeof(ScreenTransform),
            typeof(WorldToScreenProjection.AsTarget),
            typeof(ManeuveringLineReachability),
            typeof(PlanetManeuveringLineSource.AsLine),
            typeof(PlanetManeuveringLineTarget.AsLine)
        );
}

[Describe(ConceptNames.ManeuveringLine)]
public class ManeuveringLineDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity View { get; set; }
    public Entity? Target { get; set; }
}

[Apply(ConceptNames.ManeuveringLine)]
public class ManeuveringLineApplier(IConceptFactory factory) : IApplier<ManeuveringLineDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, ManeuveringLineDescription desc)
    {
        commandBuffer.Set(in entity, new Shape { Geometry = new Shape.Line(0, 0, 0), Alpha = 0f });
        commandBuffer.Set(in entity, new ScreenTransform { Z = (int)GraphicsLayer.Interface + 1 });

        factory.Make(
            World.Worlds[entity.WorldId],
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
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.PlanetManeuveringLineSource,
            new PlanetManeuveringLineSourceDescription
            {
                Planet = desc.Planet,
                Line = entity,
                View = desc.View,
            }
        );

        if (desc.Target is not { } target)
            return;

        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.PlanetManeuveringLineTarget,
            new PlanetManeuveringLineTargetDescription
            {
                Planet = target,
                Line = entity,
                View = desc.View,
            }
        );
    }
}
