using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Team = "Team";
}

[Define(ConceptNames.Team)]
public abstract class TeamDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + new Signature(
            // 阵营参考值
            typeof(TeamReferenceColor),
            // 阵营属性
            typeof(Producible),
            typeof(Combatable),
            typeof(Jumpable),
            typeof(ColonizationAbility),
            // 隶属关系
            typeof(InTeam.AsTeam),
            typeof(TeamPopulationRegistry),
            // Ai
            typeof(Ai),
            typeof(AiTimer),
            typeof(AiCooldown)
        );
}

[Describe(ConceptNames.Team)]
public class TeamDescription : IDescription
{
    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public required Color Color { get; set; }

    /// <summary>
    /// 生产一个该阵营舰船需要的工作量
    /// </summary>
    public required float Workload { get; set; }

    /// <summary>
    /// 每个该阵营的舰船每秒可以造成的伤害
    /// </summary>
    public required float Attack { get; set; }

    /// <summary>
    /// 每个该阵营的舰船最多可以承受的伤害
    /// </summary>
    public required float Health { get; set; }
}

[Apply(ConceptNames.Team)]
public class TeamApplier : IApplier<TeamDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TeamDescription desc)
    {
        commandBuffer.Set(in entity, new TeamReferenceColor { Value = desc.Color });

        commandBuffer.Set(in entity, new Producible { WorkloadPerShip = desc.Workload });

        commandBuffer.Set(
            in entity,
            new Combatable
            {
                AttackPerShipPerSecond = desc.Attack,
                MaximumDamagePerShip = desc.Health,
            }
        );

        commandBuffer.Set(in entity, new Jumpable { Speed = 100 });

        commandBuffer.Set(in entity, new ColonizationAbility { ProgressPerSecond = 1 });

        commandBuffer.Set(in entity, new Ai { Enabled = false });
        commandBuffer.Set(in entity, new AiCooldown { Duration = TimeSpan.FromSeconds(3) });
        commandBuffer.Set(in entity, new AiTimer { TimeLeft = TimeSpan.FromSeconds(2) });
    }
}
