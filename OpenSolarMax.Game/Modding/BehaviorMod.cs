using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using CsToml.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal record ConceptRelatedTypes(
    ImmutableArray<Type> DefinitionTypes,
    Type? DescriptionType,
    ImmutableArray<Type> ApplierTypes
);

internal record BehaviorMod : IDisposable
{
    public required BehaviorModInfo Metadata { get; init; }

    /// <summary>
    /// 模组中提供资产的所有文件系统
    /// </summary>
    public required ImmutableArray<IFileSystem> ContentFileSystems { get; init; }

    /// <summary>
    /// 模组中提供的参数配置文件
    /// </summary>
    public IConfigurationRoot? Configs { get; init; }

    /// <summary>
    /// 模组的入口程序集
    /// </summary>
    public required Assembly Assembly { get; init; }

    /// <summary>
    /// 模组提供的所有组件类型
    /// </summary>
    public required ImmutableArray<Type> ComponentTypes { get; init; }

    /// <summary>
    /// 模组提供的所有配置类型，按照<see cref="SchemaNameAttribute"/>索引
    /// </summary>
    public required ImmutableDictionary<
        string,
        DeclarationSchemaInfo
    > DeclarationSchemaInfos { get; init; }

    /// <summary>
    /// 游玩时的行为信息
    /// </summary>
    public required BehaviorsInfo GameplayBehaviorsInfo { get; init; }

    /// <summary>
    /// 预览时的行为信息
    /// </summary>
    public required BehaviorsInfo PreviewBehaviorsInfo { get; init; }

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

        return new BehaviorMod
        {
            Metadata = info,
            Assembly = assembly,
            ContentFileSystems = contentFileSystems.ToImmutableArray(),
            Configs = configs,
            // 查找组件类型
            ComponentTypes = assembly
                .ExportedTypes.Where(t => t.GetCustomAttribute<ComponentAttribute>() is not null)
                .ToImmutableArray(),
            // 查找关卡文件声明类型
            DeclarationSchemaInfos = Modding.FindDeclarationTypes(assembly).ToImmutableDictionary(),
            // 查找游玩场景行为相关类型
            GameplayBehaviorsInfo = new BehaviorsInfo(
                Modding
                    .FindTranslatorTypes(assembly, GameplayOrPreview.Gameplay)
                    .ToImmutableDictionary(),
                Modding
                    .FindConceptRelatedTypes(assembly, GameplayOrPreview.Gameplay)
                    .ToImmutableDictionary(),
                Modding.FindSystemTypes(assembly, GameplayOrPreview.Gameplay),
                Modding
                    .FindHookImplementations(assembly, GameplayOrPreview.Gameplay)
                    .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
            ),
            // 查找预览场景行为相关类型
            PreviewBehaviorsInfo = new BehaviorsInfo(
                Modding
                    .FindTranslatorTypes(assembly, GameplayOrPreview.Preview)
                    .ToImmutableDictionary(),
                Modding
                    .FindConceptRelatedTypes(assembly, GameplayOrPreview.Preview)
                    .ToImmutableDictionary(),
                Modding.FindSystemTypes(assembly, GameplayOrPreview.Preview),
                Modding
                    .FindHookImplementations(assembly, GameplayOrPreview.Preview)
                    .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
            ),
        };
    }
}
