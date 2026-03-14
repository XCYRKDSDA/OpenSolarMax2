using System.Collections.Immutable;
using System.Reflection;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

internal class BehaviorsInfo(
    ImmutableDictionary<string, DeclarationSchemaInfo> declarationSchemaInfos,
    ImmutableDictionary<string, ConceptRelatedTypes> conceptTypes,
    ImmutableSystemTypeCollection systemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> hookImplMethods)
{
    /// <summary>
    /// 模组提供的所有配置类型，按照<see cref="SchemaNameAttribute"/>索引
    /// </summary>
    public ImmutableDictionary<string, DeclarationSchemaInfo> DeclarationSchemaInfos { get; } = declarationSchemaInfos;

    /// <summary>
    /// 模组提供的所有概念的定义、描述和应用器
    /// </summary>
    public ImmutableDictionary<string, ConceptRelatedTypes> ConceptTypes { get; } = conceptTypes;

    /// <summary>
    /// 模组提供的所有系统类型
    /// </summary>
    public ImmutableSystemTypeCollection SystemTypes { get; } = systemTypes;

    /// <summary>
    /// 模组提供的所有钩子函数实现
    /// </summary>
    public ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods { get; } = hookImplMethods;
}
