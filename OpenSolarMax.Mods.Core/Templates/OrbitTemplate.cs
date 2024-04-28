using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 预定义轨道模板。
/// 将实体配置为一个位于世界系原点的尺寸为0且周期无穷大的预定义轨道
/// </summary>
public class OrbitTemplate : ITemplate
{
    public Archetype Archetype => Archetypes.PredefinedOrbit;

    public void Apply(Entity entity)
    {
        ref var transform = ref entity.Get<RelativeTransform>();
        ref var orbit = ref entity.Get<PredefinedOrbit>();

        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        orbit.Template.Rotation = Quaternion.Identity;
        orbit.Template.Shape = new(0, 0);
        orbit.Template.Period = float.PositiveInfinity;
    }
}
