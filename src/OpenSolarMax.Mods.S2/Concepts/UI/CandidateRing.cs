using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string CandidateRing = "CandidateRing";
}

/// <summary>
/// 候选选择圈实体定义。候选圈是尚未提交为选中状态的预览圈。
/// </summary>
[Define(ConceptNames.CandidateRing)]
public abstract class CandidateRingDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(Shape),
            typeof(ScreenTransform),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted),
            typeof(WorldToScreenProjection.AsTarget),
            typeof(PlanetCandidateRing.AsRing),
            typeof(PlanetSelectionRing.AsRing)
        );
}

[Describe(ConceptNames.CandidateRing)]
public class CandidateRingDescription : IDescription
{
    public required Entity Planet { get; set; }

    public required Entity View { get; set; }
}

[Apply(ConceptNames.CandidateRing)]
public class CandidateRingApplier(
    IConceptFactory factory,
    [Section("applier:selection_ring")] IConfiguration configs
) : IApplier<CandidateRingDescription>
{
    private readonly Color _color = configs.RequireValue<Color>("color");

    public void Apply(CommandBuffer commandBuffer, Entity entity, CandidateRingDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new Shape
            {
                Geometry = new Shape.Ring(Vector2.Zero, 0, 0, 0, Shape.ThicknessAlignment.Centered),
                Fill = _color,
            }
        );
        commandBuffer.Set(in entity, new ScreenTransform());

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
            ConceptNames.PlanetCandidateRing,
            new PlanetCandidateRingDescription
            {
                Planet = desc.Planet,
                Ring = entity,
                View = desc.View,
            }
        );
    }
}
