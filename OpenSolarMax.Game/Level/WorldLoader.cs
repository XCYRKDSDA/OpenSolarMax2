using System.Collections;
using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;

namespace OpenSolarMax.Game.Level;

internal sealed class WorldLoader(IConceptFactory factory, ITranslatorRegistry translators)
{
    public IEnumerator LoadStepByStep(LevelFile level, World world, CommandBuffer commandBuffer)
    {
        // 所有经过仿真系统后处理过至少一次的实体的代号
        var satisfiedEntities = new HashSet<string>();
        // 所有有名实体
        var namedEntities = new Dictionary<string, Entity>();
        // 所有有名配置
        var namedDeclarations = new Dictionary<string, IDeclaration>();

        // 首先解析所有模板
        foreach (var (id, templateStatement) in level.Templates)
        {
            var configuration = templateStatement.BaseDecalarationIds.Select(k => namedDeclarations[k])
                                                 .Append(templateStatement.Declaration)
                                                 .Aggregate((c1, c2) => c1.Aggregate(c2));
            namedDeclarations.Add(id, configuration);
        }

        // 解析所有实体
        foreach (var (optionalId, entityStatement, num) in level.Entities)
        {
            // 如果没有对应的描述翻译器, 则跳过该实体
            // TODO: 应当应用更严格的检查
            if (!translators.TryGetBySchema(entityStatement.SchemaName, out var conceptName, out var translator))
                continue;

            var configuration = entityStatement.BaseDecalarationIds.Select(k => namedDeclarations[k])
                                               .Append(entityStatement.Declaration)
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
            var description = translator.ToDescription(configuration, namedEntities);

            // 构造实体
            for (var i = 0; i < num; i++)
            {
                // 创造实体
                var entity = factory.Make(world, commandBuffer, conceptName, description);

                // 记录实体
                if (optionalId is not null)
                    namedEntities[optionalId] = entity;
            }
        }

        yield return null;
    }
}
