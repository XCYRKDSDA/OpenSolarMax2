using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
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

    /// <summary>
    /// 每个该阵营的单位每秒可以造成的伤害
    /// </summary>
    public float? Attack { get; set; }

    /// <summary>
    /// 每个该阵营的单位最多可以承受的伤害
    /// </summary>
    public float? Health { get; set; }
}

[ConfiguratorKey("party")]
public class PartyReferenceConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Party;

    public Type ConfigurationType => typeof(PartyReferenceConfiguration);

    private readonly PartyTemplate _template = new();

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
        => _template.Apply(entity);

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var partyConfig = configuration as PartyReferenceConfiguration ?? throw new ArgumentException("Unexpected configuration type");

        if (partyConfig.Color.HasValue)
            entity.Get<PartyReferenceColor>().Value = partyConfig.Color.Value;

        if (partyConfig.Workload.HasValue)
            entity.Get<Producible>().WorkloadPerShip = partyConfig.Workload.Value;

        if (partyConfig.Attack.HasValue)
            entity.Get<Combatable>().AttackPerUnitPerSecond = partyConfig.Attack.Value;

        if (partyConfig.Health.HasValue)
            entity.Get<Combatable>().MaximumDamagePerUnit = partyConfig.Health.Value;
    }
}
