using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class PortalChargingEffectTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required EntityReference Portal { get; set; }

    public required float PortalRadius { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        //
        typeof(InPortalEffect)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        ref var registry = ref entity.Get<InPortalEffect>();

        registry.Portal = Portal;

        registry.BackFlare = world.Make(new PortalChargingBackFlareTemplate(assets)
        {
            Effect = entity.Reference(),
            Radius = PortalRadius, Color = Color.White
        }).Reference();

        registry.SurroundFlare1 = world.Make(new PortalChargingSurroundFlareTemplate(assets)
        {
            Effect = entity.Reference(),
            Radius = PortalRadius, Color = Color.White,
            Index = 0
        }).Reference();

        registry.SurroundFlare2 = world.Make(new PortalChargingSurroundFlareTemplate(assets)
        {
            Effect = entity.Reference(),
            Radius = PortalRadius, Color = Color.White,
            Index = 1
        }).Reference();

        registry.SurroundFlare3 = world.Make(new PortalChargingSurroundFlareTemplate(assets)
        {
            Effect = entity.Reference(),
            Radius = PortalRadius, Color = Color.White,
            Index = 2
        }).Reference();

        _ = world.Make(new DependenceTemplate() { Dependent = entity.Reference(), Dependency = Portal });
        _ = world.Make(new RelativeTransformTemplate()
        {
            Parent = Portal, Child = entity.Reference(),
            Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
        });
    }
}
