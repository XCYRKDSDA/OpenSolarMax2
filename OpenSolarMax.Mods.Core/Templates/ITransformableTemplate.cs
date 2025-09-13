using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OneOf;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Templates;

public interface ITransformableTemplate
{
    OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; }

    public static Signature Signature { get; } = new(
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );
}

public static class TransformableTemplateExtensions
{
    public static void Apply(this ITransformableTemplate template, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        template.Transform.Switch(
            transform => entity.Set(
                new AbsoluteTransform(transform.Translation, transform.Rotation)
            ),
            transform => _ = world.Make(new RelativeTransformTemplate()
            {
                Parent = transform.Parent,
                Child = entity,
                Translation = transform.Translation,
                Rotation = transform.Rotation
            }),
            revolution => _ = world.Make(new RevolutionTemplate()
            {
                Parent = revolution.Parent,
                Child = entity,
                Shape = revolution.Shape,
                Period = revolution.Period,
                Rotation = revolution.Rotation,
                InitPhase = revolution.InitPhase
            })
        );
    }

    public static void Apply(this ITransformableTemplate template, CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        template.Transform.Switch(
            transform => commandBuffer.Set(in entity,
                new AbsoluteTransform(transform.Translation, transform.Rotation)
            ),
            transform => _ = world.Make(commandBuffer, new RelativeTransformTemplate
            {
                Parent = transform.Parent,
                Child = entity,
                Translation = transform.Translation,
                Rotation = transform.Rotation
            }),
            revolution => _ = world.Make(commandBuffer, new RevolutionTemplate
            {
                Parent = revolution.Parent,
                Child = entity,
                Shape = revolution.Shape,
                Period = revolution.Period,
                Rotation = revolution.Rotation,
                InitPhase = revolution.InitPhase
            })
        );
    }
}
