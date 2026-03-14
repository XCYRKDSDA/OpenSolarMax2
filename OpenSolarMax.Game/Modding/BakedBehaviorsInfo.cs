using System.Collections.Immutable;
using System.Reflection;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

internal class BakedBehaviorsInfo(
    ImmutableDictionary<string, DeclarationSchemaInfo> declarationSchemaInfos,
    ImmutableDictionary<string, ConceptInfo> conceptInfos, ImmutableSortedSystemTypeCollection systemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> hookImplMethods)
{
    public ImmutableDictionary<string, DeclarationSchemaInfo> DeclarationSchemaInfos { get; } = declarationSchemaInfos;

    public ImmutableDictionary<string, ConceptInfo> ConceptInfos { get; } = conceptInfos;

    public ImmutableSortedSystemTypeCollection SystemTypes { get; } = systemTypes;

    public ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods { get; } = hookImplMethods;
}
