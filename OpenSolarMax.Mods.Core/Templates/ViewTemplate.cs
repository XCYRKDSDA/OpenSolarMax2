using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 视图模板。
/// 将实体配置为一个描述视图的对象。
/// 默认该视图拥有位于世界系原点的宽1920、高1080、纵深±1000的相机
/// </summary>
public class ViewTemplate : ITemplate, ITransformableTemplate
{
    #region Options

    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions>
        Transform { get; set; } = new AbsoluteTransformOptions();

    public Point Size { get; set; } = new(1920, 1080);

    public (float Near, float Far) Depth { get; set; } = (-1001, 1001);

    public required Entity Party { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        // 交互
        typeof(Camera),
        typeof(ManeuvaringShipsStatus),
        typeof(LevelUIContext),
        typeof(FMOD.Studio.System),
        //
        typeof(InParty.AsAffiliate)
    );

    public Archetype Archetype => _archetype;

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
    }
}
