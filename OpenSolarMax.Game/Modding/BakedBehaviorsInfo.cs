using System.Collections.Immutable;
using System.Reflection;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

internal class BakedBehaviorsInfo(
    ImmutableDictionary<string, DeclarationTranslatorInfo> translatorTypes,
    ImmutableDictionary<string, ConceptInfo> conceptInfos, ImmutableSortedSystemTypeCollection systemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> hookImplMethods)
{
    public ImmutableDictionary<string, DeclarationTranslatorInfo> TranslatorTypes { get; } = translatorTypes;

    public ImmutableDictionary<string, ConceptInfo> ConceptInfos { get; } = conceptInfos;

    public ImmutableSortedSystemTypeCollection SystemTypes { get; } = systemTypes;

    public ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods { get; } = hookImplMethods;
}
