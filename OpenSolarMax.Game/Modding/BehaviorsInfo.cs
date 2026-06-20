using System.Collections.Immutable;
using System.Reflection;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

/// <param name="DeclarationTranslatorTypes">模组提供的所有将文件声明翻译为概念描述的翻译器，按照<see cref="TranslateAttribute"/>索引</param>
/// <param name="ConceptTypes">模组提供的所有概念的定义、描述和应用器</param>
/// <param name="SystemTypes">模组提供的所有系统类型</param>
/// <param name="HookImplMethods">模组提供的所有钩子函数实现</param>
internal record BehaviorsInfo(
    ImmutableDictionary<string, DeclarationTranslatorInfo> DeclarationTranslatorTypes,
    ImmutableDictionary<string, ConceptRelatedTypes> ConceptTypes,
    ImmutableSystemTypeCollection SystemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods
);
