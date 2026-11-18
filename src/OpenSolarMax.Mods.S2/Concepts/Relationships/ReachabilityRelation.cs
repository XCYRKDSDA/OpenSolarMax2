using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ReachabilityRelation = "ReachabilityRelation";
}

[Define(ConceptNames.ReachabilityRelation)]
public abstract class ReachabilityRelationDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(typeof(ReachabilityRelation), typeof(Reachability));
}

[Describe(ConceptNames.ReachabilityRelation)]
public class ReachabilityRelationDescription : IDescription
{
    /// <summary>
    /// 出发天体实体
    /// </summary>
    public required Entity Departure { get; set; }

    /// <summary>
    /// 目的天体实体
    /// </summary>
    public required Entity Destination { get; set; }
}

[Apply(ConceptNames.ReachabilityRelation)]
public class ReachabilityRelationApplier : IApplier<ReachabilityRelationDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        ReachabilityRelationDescription desc
    )
    {
        commandBuffer.Set(in entity, new ReachabilityRelation(desc.Departure, desc.Destination));

        // 默认两类阵营都不可达
        commandBuffer.Set(in entity, new Reachability());
    }
}
