using System.Collections;
using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;

namespace OpenSolarMax.Game.Level;

internal sealed class WorldLoader(
    IAssetsManager assets, IConceptFactory factory, IReadOnlyDictionary<string, string> schemaNamesToConceptNames)
{
    public IEnumerator LoadStepByStep(LevelFile level, World world, CommandBuffer commandBuffer)
    {
        // 所有经过仿真系统后处理过至少一次的实体的代号
        var satisfiedEntities = new HashSet<string>();
        // 所有有名实体
        var namedEntities = new Dictionary<string, Entity>();
        // 所有有名配置
        var namedConfigurations = new Dictionary<string, IConfiguration>();

        // 首先解析所有模板
        foreach (var (id, templateStatement) in level.Templates)
        {
            var configuration = templateStatement.BaseConfigurationIds.Select(k => namedConfigurations[k])
                                                 .Append(templateStatement.Configuration)
                                                 .Aggregate((c1, c2) => c1.Aggregate(c2));
            namedConfigurations.Add(id, configuration);
        }

        // 解析所有实体
        foreach (var (optionalId, entityStatement, num) in level.Entities)
        {
            var configuration = entityStatement.BaseConfigurationIds.Select(k => namedConfigurations[k])
                                               .Append(entityStatement.Configuration)
                                               .Aggregate((c1, c2) => c1.Aggregate(c2));

            // 检查是否所有依赖均已满足
            if (configuration.Requirements.Any(s => !satisfiedEntities.Contains(s)))
            {
                // 有依赖的实体未初始化过，需要交给外部计算一帧
                yield return null;
                Debug.Assert(namedEntities.Values.All(e => e.GetComponentTypes().Count > 0));
                satisfiedEntities.UnionWith(namedEntities.Keys);
            }

            // 生成描述对象
            var description = configuration.ToDescription(namedEntities, assets);

            // 构造实体
            for (var i = 0; i < num; i++)
            {
                // 创造实体
                var conceptName = schemaNamesToConceptNames[entityStatement.SchemaName];
                var entity = factory.Make(world, commandBuffer, conceptName, description);

                // 记录实体
                if (optionalId is not null)
                    namedEntities[optionalId] = entity;
            }
        }

        yield return null;
    }
}
