using Arch.Core;
using Arch.Core.Extensions;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using OpenSolarMax.Mods.Core.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

using static TreeRelationshipUtils;

public class ShipConfiguration : IEntityConfiguration
{
    public string? Planet { get; set; }

    public string? Party { get; set; }
}

[ConfiguratorKey("ship")]
public class ShipConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Ship;

    public Type ConfigurationType => typeof(ShipConfiguration);

    private readonly ShipTemplate _template = new(assets);

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
        => _template.Apply(entity);

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var unitConfig = (ShipConfiguration)configuration;

        // 设置所属星球
        if (unitConfig.Planet != null)
        {
            var planetEntity = ctx.OtherEntities[unitConfig.Planet]; 
            var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(entity, planetEntity);
            RevolutionUtils.RandomlySetShipOrbitAroundPlanet(transformRelationship, planetEntity);
        }

        // 设置所属阵营
        if (unitConfig.Party != null)
            CreateTreeRelationship<Party>(ctx.OtherEntities[unitConfig.Party], entity, indexNow: true);
    }
}
