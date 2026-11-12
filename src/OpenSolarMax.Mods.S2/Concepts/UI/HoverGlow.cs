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
    public const string HoverGlow = "HoverGlow";
}

/// <summary>
/// 悬停发光实体定义
/// </summary>
[Define(ConceptNames.HoverGlow)]
public abstract class HoverGlowDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(Shape),
            typeof(ScreenTransform),
            typeof(PointedEntityHoverGlow.AsGlow),
            typeof(Dependence.AsDependent),
            typeof(WorldToScreenProjection.AsTarget)
        );
}

[Describe(ConceptNames.HoverGlow)]
public class HoverGlowDescription : IDescription
{
    public required Entity PointedEntity { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.HoverGlow)]
public class HoverGlowApplier(IConceptFactory factory) : IApplier<HoverGlowDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, HoverGlowDescription desc)
    {
        commandBuffer.Set(in entity, new Shape { Geometry = new Shape.Circle(Vector2.Zero, 0) });
        commandBuffer.Set(in entity, new ScreenTransform { Z = (int)GraphicsLayer.Interface - 1 });

        var world = World.Worlds[entity.WorldId];

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.WorldToScreenProjection,
            new WorldToScreenProjectionDescription
            {
                Source = desc.PointedEntity,
                Target = entity,
                View = desc.View,
                ProjectRotation = false,
            }
        );

        factory.Make(
            world,
            commandBuffer,
            new DependenceDescription { Dependent = entity, Dependency = desc.PointedEntity }
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PointedEntityHoverGlow,
            new PointedEntityHoverGlowDescription
            {
                PointedEntity = desc.PointedEntity,
                Glow = entity,
                View = desc.View,
            }
        );
    }
}
