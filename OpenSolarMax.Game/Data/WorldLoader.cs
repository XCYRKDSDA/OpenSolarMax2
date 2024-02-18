using Arch.Core;
using OpenSolarMax.Game.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Game.Data;

internal sealed class WorldLoader
{
    public IReadOnlyCollection<IEntityConfigurator> Configurators => _configurators.Values;

    private readonly Dictionary<Type, IEntityConfigurator> _configurators = [];

    public void RegisterConfigurator(IEntityConfigurator configurator)
    {
        if (_configurators.ContainsKey(configurator.GetType()))
            _configurators[configurator.ConfigurationType] = configurator;
        else
            _configurators.Add(configurator.ConfigurationType, configurator);
    }

    private static IEnumerable<IEntityConfiguration> GetAllConfigs(LevelStatement statement,
                                                                   IReadOnlyDictionary<string, LevelStatement> templates)
    {
        if (statement.Base == null)
            return statement.Configs;

        return Enumerable.Concat(GetAllConfigs(templates[statement.Base], templates), statement.Configs);
    }

    private static List<Type> GetAllTypes(IEnumerable<IEntityConfiguration> configs)
    {
        var record = new HashSet<Type>();
        var ans = new List<Type>();

        foreach (var config in configs)
        {
            var configType = config.GetType();

            if (record.Contains(configType))
                continue;

            ans.Add(configType);
            record.Add(configType);
        }

        return ans;
    }

    public void Load(Level level, World world)
    {
        var namedEntities = new Dictionary<string, Entity>();

        foreach (var (optionalId, entityStatment, num) in level.Entities)
        {
            // 解析引用关系，获得所有配置项
            var allConfigs = GetAllConfigs(entityStatment, level.Templates);
            var allConfigTypes = GetAllTypes(allConfigs);

            // 合成原型
            var unionArchetype = new Archetype();
            foreach (var configType in allConfigTypes)
            {
                var configurator = _configurators[configType];
                unionArchetype += configurator.Archetype;
            }

            for (var i = 0; i < num; i++)
            {
                // 创造实体
                var entity = world.Construct(unionArchetype);

                // 记录实体
                if (optionalId != null)
                {
                    if (!namedEntities.TryAdd(optionalId, entity))
                        namedEntities[optionalId] = entity;
                }

                // 初始化实体
                foreach (var configType in allConfigTypes)
                    _configurators[configType].Initialize(in entity, namedEntities);

                foreach (var config in allConfigs)
                    _configurators[config.GetType()].Configure(config, in entity, namedEntities);
            }
        }
    }
}
