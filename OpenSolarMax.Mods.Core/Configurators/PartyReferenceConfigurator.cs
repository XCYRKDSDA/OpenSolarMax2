using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Core;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class PartyReferenceConfiguration : IEntityConfiguration
{
    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// 生产一个该阵营单位需要的工作量
    /// </summary>
    public float? Workload { get; set; }
}

[ConfiguratorKey("party")]
public class PartyReferenceConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Party;

    public Type ConfigurationType => typeof(PartyReferenceConfiguration);

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        entity.Get<PartyReferenceColor>().Value = Color.White;
        entity.Get<Producible>().WorkloadPerShip = float.PositiveInfinity;
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var partyConfig = configuration as PartyReferenceConfiguration ?? throw new ArgumentException("Unexpected configuration type");

        if (partyConfig.Color.HasValue)
            entity.Get<PartyReferenceColor>().Value = partyConfig.Color.Value;

        if (partyConfig.Workload.HasValue)
            entity.Get<Producible>().WorkloadPerShip = partyConfig.Workload.Value;
    }
}
