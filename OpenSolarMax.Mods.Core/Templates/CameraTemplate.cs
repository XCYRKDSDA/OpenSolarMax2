using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 相机模板。
/// 将实体配置为一个位于世界系原点的宽1920、高1080、纵深±1000的相机
/// </summary>
public class CameraTemplate : ITemplate
{
    public Archetype Archetype => Archetypes.Camera;

    public void Apply(Entity entity)
    {
        ref var transform = ref entity.Get<RelativeTransform>();
        ref var camera = ref entity.Get<Camera>();

        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        camera.Output = new(0, 0, 1920, 1080);
        camera.ZNear = -1000; camera.ZFar = 1000;
    }
}
