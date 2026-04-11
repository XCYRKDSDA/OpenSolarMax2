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

internal record ConceptRelatedTypes(Type? Definition, Type? Description, Type? Applier);

internal class BehaviorMod : IDisposable
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
    public ImmutableDictionary<string, DeclarationSchemaInfo> DeclarationSchemaInfos { get; }

    /// <summary>
    /// 游玩时的行为信息
    /// </summary>
    public BehaviorsInfo GameplayBehaviorsInfo { get; }

    /// <summary>
    /// 预览时的行为信息
    /// </summary>
    public BehaviorsInfo PreviewBehaviorsInfo { get; }

    public BehaviorMod(BehaviorModInfo info, IReadOnlyDictionary<string, Assembly> sharedAssemblies)
    {
        Metadata = info;

        // 加载程序集
        var ctx = new ModLoadContext(info.Assembly, sharedAssemblies);
        using var dllStream = info.Assembly.Open(FileMode.Open, FileAccess.Read);
#if DEBUG
        var pdb = info
            .Assembly.Directory.EnumerateFiles($"{info.Assembly.NameWithoutExtension}.pdb")
            .FirstOrDefault();
        using var pdbStream = pdb?.Open(FileMode.Open, FileAccess.Read);
        Assembly = ctx.LoadFromStream(dllStream, pdbStream);
#else
        Assembly = ctx.LoadFromStream(dllStream);
#endif

        // 加载资产文件系统
        List<IFileSystem> contentFileSystems = [new ResourceFileSystem(Assembly)];
        if (info.Content is not null)
        {
            contentFileSystems.Add(
                new SubFileSystem(info.Content.FileSystem, info.Content.Path, owned: false)
            );
        }
        ContentFileSystems = contentFileSystems.ToImmutableArray();

        // 加载配置文件
        if (info.Configs is not null)
        {
            var configsBuilder = new ConfigurationBuilder();
            using var tomlStream = info.Configs.Open(
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            configsBuilder.AddTomlStream(tomlStream);
            Configs = configsBuilder.Build();
        }
        else
            Configs = null;

        // 查找组件类型
        ComponentTypes = Assembly
            .ExportedTypes.Where(t => t.GetCustomAttribute<ComponentAttribute>() is not null)
            .ToImmutableArray();

        // 查找关卡文件声明类型
        DeclarationSchemaInfos = Modding.FindDeclarationTypes(Assembly).ToImmutableDictionary();

        // 查找游玩场景行为相关类型
        GameplayBehaviorsInfo = new BehaviorsInfo(
            Modding
                .FindTranslatorTypes(Assembly, GameplayOrPreview.Gameplay)
                .ToImmutableDictionary(),
            Modding
                .FindConceptRelatedTypes(Assembly, GameplayOrPreview.Gameplay)
                .ToImmutableDictionary(),
            Modding.FindSystemTypes(Assembly, GameplayOrPreview.Gameplay),
            Modding
                .FindHookImplementations(Assembly, GameplayOrPreview.Gameplay)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
        );

        // 查找预览场景行为相关类型
        PreviewBehaviorsInfo = new BehaviorsInfo(
            Modding
                .FindTranslatorTypes(Assembly, GameplayOrPreview.Preview)
                .ToImmutableDictionary(),
            Modding
                .FindConceptRelatedTypes(Assembly, GameplayOrPreview.Preview)
                .ToImmutableDictionary(),
            Modding.FindSystemTypes(Assembly, GameplayOrPreview.Preview),
            Modding
                .FindHookImplementations(Assembly, GameplayOrPreview.Preview)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
        );
    }

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
}
