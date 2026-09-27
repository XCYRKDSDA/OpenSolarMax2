using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string TeamInheritance = "TeamInheritance";
}

/// <summary>
/// 阵营继承关系：子实体的阵营跟随父实体。
/// <para>
/// 复用 <see cref="InTeam"/> 作为关系树的标记类型——该关系只描述继承结构，
/// 子实体自身的阵营归属仍由系统另行建立为 <see cref="InTeam"/> 记录。
/// </para>
/// </summary>
[Define(ConceptNames.TeamInheritance), BothForGameplayAndPreview]
public abstract class TeamInheritanceDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(TreeRelationship<InTeam>));
}

[Describe(ConceptNames.TeamInheritance), BothForGameplayAndPreview]
public class TeamInheritanceDescription : IDescription
{
    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }
}

[Apply(ConceptNames.TeamInheritance), BothForGameplayAndPreview]
public class TeamInheritanceApplier : IApplier<TeamInheritanceDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TeamInheritanceDescription desc)
    {
        commandBuffer.Set(in entity, new TreeRelationship<InTeam>(desc.Parent, desc.Child));
    }
}
