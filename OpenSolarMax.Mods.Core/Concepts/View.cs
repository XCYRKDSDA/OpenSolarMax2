using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string View = "View";
}

[Define(ConceptNames.View)]
public abstract class ViewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature +
        new Signature(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 交互
            typeof(Camera),
            typeof(ManeuvaringShipsStatus),
            typeof(FMOD.Studio.System),
            typeof(Viewport),
            typeof(PreviewStatus),
            //
            typeof(InParty.AsAffiliate),
            // UI 插件
            typeof(TotalPopulationWidget),
            // 视图标识
            typeof(ViewTag)
        );
}

[Describe(ConceptNames.View)]
public class ViewDescription : IDescription
{
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    public Point Size { get; set; } = new(1920, 1080);

    public (float Near, float Far) Depth { get; set; } = (-1001, 1001);

    public required Entity Party { get; set; }
}

[Apply(ConceptNames.View)]
public class ViewApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<ViewDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ViewDescription desc)
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

        // 设置阵营
        factory.Make(world, commandBuffer, ConceptNames.InParty,
                     new InPartyDescription { Party = desc.Party, Affiliate = entity });

        // 初始化 UI
        commandBuffer.Set(in entity, new TotalPopulationWidget(assets));
    }
}
