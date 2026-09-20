using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 沿阵营继承关系树自根向叶传播阵营，维护子实体自身的 InTeam 关系
/// </summary>
[SimulateSystem, LateUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(TreeRelationship<InTeam>.AsParent)),
    ReadCurr(typeof(TreeRelationship<InTeam>.AsChild)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    DelayedCalc
]
public sealed partial class SynchronizeTeamSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    private static readonly QueryDescription _parentsDesc =
        new QueryDescription().WithAll<TreeRelationship<InTeam>.AsParent>();

    private void SyncToChildren(Entity entity, Entity inheritedTeam, CommandBuffer commandBuffer)
    {
        // 实体自身的阵营优先，其次继承自父实体
        var team =
            entity.TryGet<InTeam.AsAffiliate>(out var asAffiliate)
            && asAffiliate.Relationship is not null
                ? asAffiliate.Relationship.Value.Copy.Team
                : inheritedTeam;

        if (!entity.TryGet<TreeRelationship<InTeam>.AsParent>(out var asParent))
            return;

        foreach (var (_, record) in asParent.Relationships)
        {
            var child = record.Child;
            if (!child.IsAlive())
                continue;

            // 维护子实体的阵营归属（子实体必须带 AsAffiliate 索引组件才能被索引）
            if (child.TryGet<InTeam.AsAffiliate>(out var childAffiliate))
            {
                var current = childAffiliate.Relationship is not null
                    ? childAffiliate.Relationship.Value.Copy.Team
                    : Entity.Null;

                if (current != team)
                {
                    if (childAffiliate.Relationship is not null)
                        commandBuffer.Destroy(childAffiliate.Relationship.Value.Ref);

                    if (team != Entity.Null)
                        factory.Make(
                            world,
                            commandBuffer,
                            ConceptNames.InTeam,
                            new InTeamDescription { Team = team, Affiliate = child }
                        );
                }
            }

            // 自根向叶递归，保证多级子实体拿到本帧已更新的队伍
            SyncToChildren(child, team, commandBuffer);
        }
    }

    public void Update(CommandBuffer commandBuffer)
    {
        var query = world.Query(in _parentsDesc);
        foreach (var chunk in query.GetChunkIterator())
        {
            foreach (var idx in chunk)
            {
                var entity = chunk.Entity(idx);

                // 仅从根实体开始；非根实体由其父实体递归处理
                if (
                    entity.TryGet<TreeRelationship<InTeam>.AsChild>(out var asChild)
                    && asChild.Relationship is not null
                )
                    continue;

                SyncToChildren(entity, Entity.Null, commandBuffer);
            }
        }
    }
}
