using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string ViewPreview = "ViewPreview";
}

[Define(ConceptNames.ViewPreview), OnlyForPreview]
public abstract class ViewPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new Signature(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 渲染
            typeof(Camera),
            typeof(Viewport),
            typeof(RenderSettings),
            // 视图标识
            typeof(ViewTag)
        );
}

[Describe(ConceptNames.ViewPreview), OnlyForPreview]
public class ViewPreviewDescription : IDescription
{
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    public Point Size { get; set; } = new(1920, 1080);

    public (float Near, float Far) Depth { get; set; } = (-1001, 1001);
}

[Apply(ConceptNames.ViewPreview), OnlyForPreview]
public class ViewPreviewApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<ViewPreviewDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ViewPreviewDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 设置相机尺寸
        commandBuffer.Set(in entity, new Camera
        {
            Width = desc.Size.X,
            Height = desc.Size.Y,
            ZNear = desc.Depth.Near,
            ZFar = desc.Depth.Far
        });
    }
}
