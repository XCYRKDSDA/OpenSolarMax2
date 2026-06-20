using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using OpenSolarMax.Game.Modding.Declaration;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal record ConceptRelatedTypes(Type? Definition, Type? Description, Type? Applier);

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
}
