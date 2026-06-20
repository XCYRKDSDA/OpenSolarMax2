using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using CsToml.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Myra.Graphics2D.UI;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal record ConceptRelatedTypes(
    ImmutableArray<Type> DefinitionTypes,
    Type? DescriptionType,
    ImmutableArray<Type> ApplierTypes
);

/// <param name="ContentFileSystems">模组中提供资产的所有文件系统</param>
/// <param name="Configs">模组中提供的参数配置文件</param>
/// <param name="Assembly">模组的入口程序集</param>
/// <param name="ComponentTypes">模组提供的所有组件类型</param>
/// <param name="DeclarationSchemaInfos">模组提供的所有配置类型，按照<see cref="SchemaNameAttribute"/>索引</param>
/// <param name="GameplayBehaviorsInfo">游玩时的行为信息</param>
/// <param name="PreviewBehaviorsInfo">预览时的行为信息</param>
internal record BehaviorMod(
    BehaviorModInfo Metadata,
    ImmutableArray<IFileSystem> ContentFileSystems,
    IConfigurationRoot? Configs,
    Assembly Assembly,
    ImmutableArray<Type> ComponentTypes,
    ImmutableDictionary<string, DeclarationSchemaInfo> DeclarationSchemaInfos,
    BehaviorsInfo GameplayBehaviorsInfo,
    BehaviorsInfo PreviewBehaviorsInfo
) : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        // 启动 ALC 卸载
        AssemblyLoadContext.GetLoadContext(Assembly)!.Unload();

        // 释放资产文件系统
        foreach (var fs in ContentFileSystems)
            fs.Dispose();

        _disposed = true;
    }

    public static BehaviorMod Load(
        BehaviorModInfo info,
        IReadOnlyDictionary<string, Assembly> sharedAssemblies
    )
    {
        // 加载程序集
        var ctx = new ModLoadContext(info.Assembly, sharedAssemblies);
        using var dllStream = info.Assembly.Open(FileMode.Open, FileAccess.Read);
#if DEBUG
        var pdb = info
            .Assembly.Directory.EnumerateFiles($"{info.Assembly.NameWithoutExtension}.pdb")
            .FirstOrDefault();
        using var pdbStream = pdb?.Open(FileMode.Open, FileAccess.Read);
        var assembly = ctx.LoadFromStream(dllStream, pdbStream);
#else
        var assembly = ctx.LoadFromStream(dllStream);
#endif

        // 加载资产文件系统
        List<IFileSystem> contentFileSystems = [new ResourceFileSystem(assembly)];
        if (info.Content is not null)
        {
            contentFileSystems.Add(
                new SubFileSystem(info.Content.FileSystem, info.Content.Path, owned: false)
            );
        }

        // 加载配置文件
        IConfigurationRoot? configs = null;
        if (info.Configs is not null)
        {
            var configsBuilder = new ConfigurationBuilder();
            using var tomlStream = info.Configs.Open(
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            configsBuilder.AddTomlStream(tomlStream);
            configs = configsBuilder.Build();
        }

        // 查找组件类型
        var componentTypes = assembly
            .ExportedTypes.Where(t => t.GetCustomAttribute<ComponentAttribute>() is not null)
            .ToImmutableArray();
        // 查找关卡文件声明类型
        var declarationSchemaInfos = FindDeclarationTypes(assembly).ToImmutableDictionary();
        // 查找游玩场景行为相关类型
        var gameplayBehaviorsInfo = new BehaviorsInfo(
            FindTranslatorTypes(assembly, GameplayOrPreview.Gameplay).ToImmutableDictionary(),
            FindConceptRelatedTypes(assembly, GameplayOrPreview.Gameplay).ToImmutableDictionary(),
            FindSystemTypes(assembly, GameplayOrPreview.Gameplay),
            FindHookImplementations(assembly, GameplayOrPreview.Gameplay)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
        );
        // 查找预览场景行为相关类型
        var previewBehaviorsInfo = new BehaviorsInfo(
            FindTranslatorTypes(assembly, GameplayOrPreview.Preview).ToImmutableDictionary(),
            FindConceptRelatedTypes(assembly, GameplayOrPreview.Preview).ToImmutableDictionary(),
            FindSystemTypes(assembly, GameplayOrPreview.Preview),
            FindHookImplementations(assembly, GameplayOrPreview.Preview)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
        );

        return new BehaviorMod(
            info,
            contentFileSystems.ToImmutableArray(),
            configs,
            assembly,
            componentTypes,
            declarationSchemaInfos,
            gameplayBehaviorsInfo,
            previewBehaviorsInfo
        );
    }

    #region 反射扫描

    /// <summary>
    /// 获取行为类型所应用的场景
    /// </summary>
    /// <param name="type">行为类型</param>
    /// <returns>场景。一个行为类型可能适用于多个场景</returns>
    private static GameplayOrPreview GetBehaviorTypeScene(Type type)
    {
        return type.GetCustomAttribute<BothForGameplayAndPreviewAttribute>() is not null
                ? GameplayOrPreview.Preview | GameplayOrPreview.Gameplay
            : type.GetCustomAttribute<OnlyForPreviewAttribute>() is not null
                ? GameplayOrPreview.Preview
            : GameplayOrPreview.Gameplay;
    }

    /// <summary>
    /// 从一个程序集中找到所有的配置器类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>所有配置器的类型和其对应的键值</returns>
    private static Dictionary<string, DeclarationSchemaInfo> FindDeclarationTypes(Assembly assembly)
    {
        var configurationTypes = new Dictionary<string, DeclarationSchemaInfo>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (!type.GetInterfaces().Contains(typeof(IDeclaration)))
                continue;

            var schemaNameAttr =
                type.GetCustomAttribute<SchemaNameAttribute>()
                ?? throw new Exception($"Can't find attribute `SchemaName` in type {type.Name}");

            configurationTypes.Add(
                schemaNameAttr.Name,
                new DeclarationSchemaInfo(type, schemaNameAttr.Name)
            );
        }

        return configurationTypes;
    }

    private static Dictionary<string, DeclarationTranslatorInfo> FindTranslatorTypes(
        Assembly assembly,
        GameplayOrPreview scene
    )
    {
        var translators = new Dictionary<string, DeclarationTranslatorInfo>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (!type.GetInterfaces().Contains(typeof(ITranslator)))
                continue;

            if ((GetBehaviorTypeScene(type) & scene) == 0)
                continue;

            var translateAttr =
                type.GetCustomAttribute<TranslateAttribute>()
                ?? throw new Exception($"Can't find attribute `Translate` in type {type.Name}");
            translators.Add(
                translateAttr.SchemaName,
                new DeclarationTranslatorInfo(
                    type,
                    translateAttr.SchemaName,
                    translateAttr.ConceptName
                )
            );
        }

        return translators;
    }

    private static Dictionary<string, ConceptRelatedTypes> FindConceptRelatedTypes(
        Assembly assembly,
        GameplayOrPreview scene
    )
    {
        var definitionTypes = new Dictionary<string, Type>();
        var descriptionTypes = new Dictionary<string, Type>();
        var applierTypes = new Dictionary<string, Type>();
        foreach (var type in assembly.GetExportedTypes())
        {
            // 筛选符合场景要求的概念类型
            if ((GetBehaviorTypeScene(type) & scene) == 0)
                continue;

            if (type.GetInterfaces().Contains(typeof(IDefinition)))
            {
                var name = type.GetCustomAttribute<DefineAttribute>()!.Key;
                definitionTypes.Add(name, type);
            }
            else if (type.GetInterfaces().Contains(typeof(IDescription)))
            {
                var name = type.GetCustomAttribute<DescribeAttribute>()!.Key;
                descriptionTypes.Add(name, type);
            }
            else if (
                type.GetInterfaces().Contains(typeof(IApplier))
                || type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IApplier<>))
            )
            {
                var name = type.GetCustomAttribute<ApplyAttribute>()!.Key;
                applierTypes.Add(name, type);
            }
        }

        var allConceptNames = definitionTypes
            .Keys.Concat(descriptionTypes.Keys)
            .Concat(applierTypes.Keys)
            .ToHashSet();
        return allConceptNames.ToDictionary(
            k => k,
            k => new ConceptRelatedTypes(
                definitionTypes.TryGetValue(k, out var d) ? [d] : [],
                descriptionTypes.GetValueOrDefault(k),
                applierTypes.TryGetValue(k, out var a) ? [a] : []
            )
        );
    }

    /// <summary>
    /// 从一个程序集中找到所有的系统类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="scene"></param>
    /// <returns>各种类型系统类型的集合</returns>
    private static ImmutableSystemTypeCollection FindSystemTypes(
        Assembly assembly,
        GameplayOrPreview scene
    )
    {
        var systemTypes = new SystemTypeCollection();

        foreach (var type in assembly.GetExportedTypes())
        {
            // 排除抽象类、接口、泛型类
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                continue;

            // 筛选系统类型
            if (
                !type.GetInterfaces()
                    .Intersect([
                        typeof(ITickSystem),
                        typeof(ITickSystemWithStructuralChanges),
                        typeof(ICalcSystem),
                        typeof(ICalcSystemWithStructuralChanges),
                    ])
                    .Any()
            )
                continue;

            // 排除禁用的系统
            if (type.GetCustomAttribute<DisableAttribute>() is not null)
                continue;

            // 筛选符合场景要求的系统
            if ((GetBehaviorTypeScene(type) & scene) == 0)
                continue;

            if (type.GetCustomAttribute<SimulateSystemAttribute>() is not null)
                systemTypes.Simulate.Add(type);

            if (type.GetCustomAttribute<InputSystemAttribute>() is not null)
                systemTypes.Input.Add(type);

            if (type.GetCustomAttribute<AiSystemAttribute>() is not null)
                systemTypes.Ai.Add(type);

            if (type.GetCustomAttribute<RenderSystemAttribute>() is not null)
                systemTypes.Render.Add(type);
        }

        return systemTypes.ToImmutableSystemTypeCollection();
    }

    /// <summary>
    /// 从一个程序集中找到所有的 Hook 实现方法
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="scene"></param>
    /// <returns></returns>
    private static ILookup<string, MethodInfo> FindHookImplementations(
        Assembly assembly,
        GameplayOrPreview scene
    )
    {
        const BindingFlags implFlags = BindingFlags.Public | BindingFlags.Static;
        return assembly
            .GetExportedTypes()
            .Where(t => t.GetCustomAttributes<HookProviderAttribute>().Any())
            .Where(t => (GetBehaviorTypeScene(t) & scene) != 0)
            .SelectMany(t => t.GetMethods(implFlags))
            .SelectMany(
                m => m.GetCustomAttributes<HookOnAttribute>(),
                (m, a) => (hook: a.Hook, method: m)
            )
            .ToLookup(p => p.hook, p => p.method);
    }

    /// <summary>
    /// 从一个程序集中找到所有的关卡界面控件类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    private static IEnumerable<KeyValuePair<LevelWidgetAttribute, Type>> FindLevelWidgetTypes(
        Assembly assembly
    )
    {
        return assembly
            .ExportedTypes.Where(t =>
                t.GetCustomAttribute<LevelWidgetAttribute>() is not null
                && t.IsSubclassOf(typeof(Widget))
            )
            .Select(t => new KeyValuePair<LevelWidgetAttribute, Type>(
                t.GetCustomAttribute<LevelWidgetAttribute>()!,
                t
            ));
    }

    #endregion
}
