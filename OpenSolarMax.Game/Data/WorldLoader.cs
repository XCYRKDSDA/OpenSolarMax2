using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Game.Data;

internal sealed class WorldLoader(IAssetsManager assets)
{
    private static IEntityConfiguration[] GetAllConfigs(
        ConfigurationStatement statement, IReadOnlyDictionary<string, IEntityConfiguration[]> cache)
    {
        if (statement.Bases.Length == 0)
            return statement.Configurations;

        var configs = cache[statement.Bases[0]];
        for (var i = 1; i < statement.Bases.Length; i++)
            configs = Aggregate(configs, cache[statement.Bases[i]]);
        configs = Aggregate(configs, statement.Configurations);
        return configs;
    }

    private static IEntityConfiguration[] Aggregate(IEntityConfiguration[] cfgs, IEntityConfiguration[] newCfgs)
    {
        Debug.Assert(cfgs.Length == newCfgs.Length);
        return cfgs.Zip(newCfgs).Select(pair => pair.First.Aggregate(pair.Second)).ToArray();
    }

    public void Load(Level level, World world)
    {
        var namedTemplates = new Dictionary<string, ITemplate[]>();
        var namedEntities = new Dictionary<string, Entity>();
        var ctx = new WorldLoadingContext(namedTemplates, namedEntities);

        var cache = new Dictionary<string, IEntityConfiguration[]>();

        // 首先解析所有模板
        foreach (var (id, templateStatement) in level.Templates)
        {
            // 解析引用关系，获得所有配置项
            var allConfigs = GetAllConfigs(templateStatement, cache);
            cache[id] = allConfigs;

            var allTemplates = allConfigs.Select(c => c.ToTemplate(ctx, assets)).ToArray();
            namedTemplates.Add(id, allTemplates);
        }

        // 解析所有实体
        foreach (var (optionalId, entityStatement, num) in level.Entities)
        {
            // 解析引用关系，获得所有配置项
            var allConfigs = GetAllConfigs(entityStatement, cache);
            if (optionalId is not null)
                cache[optionalId] = allConfigs;

            var allTemplates = allConfigs.Select(c => c.ToTemplate(ctx, assets)).ToArray();

            // 合成原型
            var unionArchetype = new Archetype();
            foreach (var template in allTemplates)
                unionArchetype += template.Archetype;

            // 构造实体
            for (var i = 0; i < num; i++)
            {
                // 创造实体
                var entity = world.Construct(unionArchetype);

                // 记录实体
                if (optionalId is not null)
                    namedEntities[optionalId] = entity;

                // 配置实体
                foreach (var template in allTemplates)
                    template.Apply(entity);
            }
        }
    }
}
