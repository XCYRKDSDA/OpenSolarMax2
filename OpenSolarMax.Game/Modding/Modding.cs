using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Myra.Graphics2D.UI;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal static partial class Modding
{
    public static string DefaultPreviewPattern => "preview.*";

    public static string DefaultBackgroundPattern => "background.*";

    public static string DefaultAssemblyFormat => "{}.dll";

    public static string DefaultContentDir => "Content";

    public static string DefaultConfigsFile => "configs.toml";

    public static string DefaultLevelsDir => "Levels";

    private static List<(DirectoryEntry, ModManifest)> FindAllModManifests(
        DirectoryEntry dir,
        ModType type
    )
    {
        var result = new List<(DirectoryEntry, ModManifest)>();
        var options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        foreach (var subDir in dir.EnumerateDirectories())
        {
            var manifestFile = subDir.EnumerateFiles("manifest.json").FirstOrDefault();
            if (manifestFile is null)
                continue;

            using var stream = manifestFile.Open(FileMode.Open, FileAccess.Read);
            var manifest =
                JsonSerializer.Deserialize<ModManifest>(stream, options)
                ?? throw new JsonException();
            if (manifest.Type != type)
                continue;

            result.Add((subDir, manifest));
        }

        return result;
    }

    public static List<BehaviorModInfo> ListBehaviorMods()
    {
        return FindAllModManifests(Folders.Mods.Behaviors.GetDirectoryEntry("/"), ModType.Behavior)
            .Select(pair => new BehaviorModInfo(pair.Item1, pair.Item2))
            .ToList();
    }

    public static List<ContentModInfo> ListContentMods()
    {
        return FindAllModManifests(Folders.Mods.Levels.GetDirectoryEntry("/"), ModType.Content)
            .Select(pair => new ContentModInfo(pair.Item1, pair.Item2))
            .ToList();
    }

    public static List<LevelModInfo> ListLevelMods()
    {
        return FindAllModManifests(Folders.Mods.Levels.GetDirectoryEntry("/"), ModType.Levels)
            .Select(pair => new LevelModInfo(pair.Item1, pair.Item2))
            .ToList();
    }

    /// <summary>
    /// 获取行为类型所应用的场景
    /// </summary>
    /// <param name="type">行为类型</param>
    /// <returns>场景。一个行为类型可能适用于多个场景</returns>
    public static GameplayOrPreview GetBehaviorTypeScene(Type type)
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
    public static Dictionary<string, DeclarationSchemaInfo> FindDeclarationTypes(Assembly assembly)
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

    public static Dictionary<string, DeclarationTranslatorInfo> FindTranslatorTypes(
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

    public static Dictionary<string, ConceptRelatedTypes> FindConceptRelatedTypes(
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
                definitionTypes.GetValueOrDefault(k),
                descriptionTypes.GetValueOrDefault(k),
                applierTypes.GetValueOrDefault(k)
            )
        );
    }

    /// <summary>
    /// 从一个程序集中找到所有的系统类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="scene"></param>
    /// <returns>各种类型系统类型的集合</returns>
    public static ImmutableSystemTypeCollection FindSystemTypes(
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
    public static ILookup<string, MethodInfo> FindHookImplementations(
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
    public static IEnumerable<KeyValuePair<LevelWidgetAttribute, Type>> FindLevelWidgetTypes(
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
}
