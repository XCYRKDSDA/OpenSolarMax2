using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 视图模板。
/// 将实体配置为一个描述视图的对象。
/// 默认该视图拥有位于世界系原点的宽1920、高1080、纵深±1000的相机
/// </summary>
public class ViewTemplate(IAssetsManager assets) : ITemplate, ITransformableTemplate
{
    private static readonly Signature _signature = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
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

    public Signature Signature => _signature;

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿
        (this as ITransformableTemplate).Apply(entity);

        // 设置相机尺寸
        ref var camera = ref entity.Get<Camera>();
        camera.Width = Size.X;
        camera.Height = Size.Y;
        camera.ZNear = Depth.Near;
        camera.ZFar = Depth.Far;

        // 设置阵营
        var inPartyTemplate = new InPartyTemplate() { Party = Party, Affiliate = entity };
        _ = world.Make(inPartyTemplate);

        // 初始化 UI
        entity.Set(new TotalPopulationWidget(assets));
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿
        (this as ITransformableTemplate).Apply(commandBuffer, entity);

        // 设置相机尺寸
        commandBuffer.Set(in entity, new Camera
        {
            Width = Size.X,
            Height = Size.Y,
            ZNear = Depth.Near,
            ZFar = Depth.Far
        });

        // 设置阵营
        world.Make(commandBuffer, new InPartyTemplate { Party = Party, Affiliate = entity });

        // 初始化 UI
        commandBuffer.Set(in entity, new TotalPopulationWidget(assets));
    }

    #region Options

    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions>
        Transform { get; set; } = new AbsoluteTransformOptions();

    public Point Size { get; set; } = new(1920, 1080);

    public (float Near, float Far) Depth { get; set; } = (-1001, 1001);

    public required Entity Party { get; set; }

    #endregion
}
