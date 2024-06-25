using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 视图模板。
/// 将实体配置为一个描述视图的对象。
/// 默认该视图拥有位于世界系原点的宽1920、高1080、纵深±1000的相机
/// </summary>
public class ViewTemplate : ITemplate
{
    public Archetype Archetype => Archetypes.View;

    public void Apply(Entity entity)
    {
        ref var transform = ref entity.Get<RelativeTransform>();
        ref var camera = ref entity.Get<Camera>();

        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        camera.Output = new(0, 0, 1920, 1080);
        camera.ZNear = -1001;
        camera.ZFar = 1001;
    }
}
