using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Party = "Party";
}

[Define(ConceptNames.Party)]
public abstract class PartyDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature +
        new Signature(
            // 阵营参考值
            typeof(PartyReferenceColor),
            // 阵营属性
            typeof(Producible),
            typeof(Combatable),
            typeof(Shippable),
            typeof(ColonizationAbility),
            // 隶属关系
            typeof(InParty.AsParty),
            typeof(PartyPopulationRegistry),
            // Ai
            typeof(Ai),
            typeof(AiTimer),
            typeof(AiCooldown)
        );
}

[Describe(ConceptNames.Party)]
public class PartyDescription : IDescription
{
    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public required Color Color { get; set; }

    /// <summary>
    /// 生产一个该阵营单位需要的工作量
    /// </summary>
    public required float Workload { get; set; }

    /// <summary>
    /// 每个该阵营的单位每秒可以造成的伤害
    /// </summary>
    public required float Attack { get; set; }

    /// <summary>
    /// 每个该阵营的单位最多可以承受的伤害
    /// </summary>
    public required float Health { get; set; }
}

[Apply(ConceptNames.Party)]
public class PartyApplier : IApplier<PartyDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, PartyDescription desc)
    {
        commandBuffer.Set(in entity, new PartyReferenceColor { Value = desc.Color });

        commandBuffer.Set(in entity, new Producible { WorkloadPerShip = desc.Workload });

        commandBuffer.Set(in entity, new Combatable
        {
            AttackPerUnitPerSecond = desc.Attack,
            MaximumDamagePerUnit = desc.Health
        });

        commandBuffer.Set(in entity, new Shippable { Speed = 100 });

        commandBuffer.Set(in entity, new ColonizationAbility { ProgressPerSecond = 1 });

        commandBuffer.Set(in entity, new Ai { Enabled = true });
        commandBuffer.Set(in entity, new AiCooldown { Duration = TimeSpan.FromSeconds(3) });
        commandBuffer.Set(in entity, new AiTimer { TimeLeft = TimeSpan.FromSeconds(2) });
    }
}
