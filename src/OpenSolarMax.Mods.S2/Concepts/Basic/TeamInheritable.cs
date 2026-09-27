using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string TeamInheritable = "TeamInheritable";
}

[Define(ConceptNames.TeamInheritable), BothForGameplayAndPreview]
public abstract class TeamInheritableDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(InTeam.AsAffiliate),
            typeof(TreeRelationship<InTeam>.AsChild),
            typeof(TreeRelationship<InTeam>.AsParent)
        );
}

[Describe(ConceptNames.TeamInheritable), BothForGameplayAndPreview]
public class TeamInheritableDescription : IDescription
{
    /// <summary>
    /// 直接隶属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 阵营继承的来源实体。仅在未直接隶属阵营时生效
    /// </summary>
    public Entity TeamSource { get; set; } = Entity.Null;
}

[Apply(ConceptNames.TeamInheritable), BothForGameplayAndPreview]
public class TeamInheritableApplier(IConceptFactory factory) : IApplier<TeamInheritableDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TeamInheritableDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        if (desc.Team != Entity.Null)
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.InTeam,
                new InTeamDescription { Team = desc.Team, Affiliate = entity }
            );
        else if (desc.TeamSource != Entity.Null)
            factory.Make(
                world,
                commandBuffer,
                new TeamInheritanceDescription { Parent = desc.TeamSource, Child = entity }
            );
    }
}
