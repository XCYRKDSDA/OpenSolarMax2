using Arch.Core;
using OpenSolarMax.Core.Utils;
using Archetype = OpenSolarMax.Core.Utils.Archetype;

namespace OpenSolarMax.Game.Data;

internal sealed class WorldLoader
{
    public IReadOnlyCollection<IEntityConfigurator> Configurators => _configurators.Values;

    private readonly Dictionary<Type, IEntityConfigurator> _configurators = [];

    public void RegisterConfigurator<T>(T configurator) where T : IEntityConfigurator
    {
        if (_configurators.ContainsKey(typeof(T)))
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

        foreach (var (optionalId, entityStatment) in level.Entities)
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

            // 创造实体
            var entity = world.Construct(unionArchetype);
            if (optionalId != null)
                namedEntities.Add(optionalId, entity);

            // 初始化实体
            foreach (var configType in allConfigTypes)
                _configurators[configType].Initialize(in entity, namedEntities);

            foreach (var config in entityStatment.Configs)
                _configurators[config.GetType()].Configure(config, in entity, namedEntities);
        }
    }
}
