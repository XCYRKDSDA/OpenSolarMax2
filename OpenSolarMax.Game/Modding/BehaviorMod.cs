using System.Collections.Immutable;
using System.Reflection;
using CsToml.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal record ConceptRelatedTypes(Type? Definition, Type? Description, Type? Applier);

internal class BehaviorMod
{
    public BehaviorModInfo Metadata { get; }

    /// <summary>
    /// 模组中提供资产的所有文件系统
    /// </summary>
    public ImmutableArray<IFileSystem> ContentFileSystems { get; }

    /// <summary>
    /// 模组中提供的参数配置文件
    /// </summary>
    public IConfigurationRoot? Configs { get; }

    /// <summary>
    /// 模组的入口程序集
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// 模组提供的所有组件类型
    /// </summary>
    public ImmutableArray<Type> ComponentTypes { get; }

    /// <summary>
    /// 模组提供的所有配置类型，按照<see cref="SchemaNameAttribute"/>索引
    /// </summary>
    public ImmutableDictionary<string, ConfigurationSchemaInfo> ConfigurationSchemaInfos { get; }

    /// <summary>
    /// 模组提供的所有概念的定义、描述和应用器
    /// </summary>
    public ImmutableDictionary<string, ConceptRelatedTypes> ConceptTypes { get; }

    /// <summary>
    /// 模组提供的所有系统类型
    /// </summary>
    public ImmutableSystemTypeCollection SystemTypes { get; }

    /// <summary>
    /// 模组提供的所有钩子函数实现
    /// </summary>
    public ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods { get; }

    public BehaviorMod(BehaviorModInfo info, IReadOnlyDictionary<string, Assembly> sharedAssemblies)
    {
        Metadata = info;

        // 加载程序集
        var ctx = new ModLoadContext(info.Assembly, sharedAssemblies);
        using var dllStream = info.Assembly.Open(FileMode.Open, FileAccess.Read);
#if DEBUG
        var pdb = info.Assembly.Directory.EnumerateFiles($"{info.Assembly.NameWithoutExtension}.pdb").FirstOrDefault();
        using var pdbStream = pdb?.Open(FileMode.Open, FileAccess.Read);
        Assembly = ctx.LoadFromStream(dllStream, pdbStream);
#else
        Assembly = ctx.LoadFromStream(dllStream);
#endif

        // 加载资产文件系统
        List<IFileSystem> contentFileSystems = [new ResourceFileSystem(Assembly)];
        if (info.Content is not null)
            contentFileSystems.Add(new SubFileSystem(info.Content.FileSystem, info.Content.Path));
        ContentFileSystems = contentFileSystems.ToImmutableArray();

        // 加载配置文件
        if (info.Configs is not null)
        {
            var configsBuilder = new ConfigurationBuilder();
            using var tomlStream = info.Configs.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            configsBuilder.AddTomlStream(tomlStream);
            Configs = configsBuilder.Build();
        }
        else
            Configs = null;

        // 查找组件类型
        ComponentTypes = Assembly.ExportedTypes.Where(t => t.GetCustomAttribute<ComponentAttribute>() is not null)
                                 .ToImmutableArray();

        // 查找配置类型
        ConfigurationSchemaInfos = Modding.FindConfigurationTypes(Assembly).ToImmutableDictionary();

        // 查找概念类型
        ConceptTypes = Modding.FindConceptRelatedTypes(Assembly).ToImmutableDictionary();

        // 查找所有系统
        SystemTypes = Modding.FindSystemTypes(Assembly);

        // 查找所有 Hook 实现
        HookImplMethods = Modding.FindHookImplementations(Assembly)
                                 .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray());
    }
}
