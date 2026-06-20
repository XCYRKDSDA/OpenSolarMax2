using System.Collections.Immutable;
using System.Reflection;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

internal class BakedBehaviorsInfo(
    ImmutableDictionary<string, DeclarationTranslatorInfo> translatorTypes,
    ImmutableDictionary<string, ConceptInfo> conceptInfos,
    ImmutableSortedSystemTypeCollection systemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> hookImplMethods
)
{
    public ImmutableDictionary<string, DeclarationTranslatorInfo> TranslatorTypes { get; } =
        translatorTypes;

    public ImmutableDictionary<string, ConceptInfo> ConceptInfos { get; } = conceptInfos;

    public ImmutableSortedSystemTypeCollection SystemTypes { get; } = systemTypes;

    public ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods { get; } =
        hookImplMethods;

    private static ImmutableSortedSystemTypes BakeSortedSystemTypes(IReadOnlySet<Type> systemTypes)
    {
        var orders = SystemsTopology.ExtractExecutionOrders(systemTypes);
        var sorted = SystemsTopology.TopologicalSortSystems(systemTypes, orders);
        return new ImmutableSortedSystemTypes([.. systemTypes], [.. orders], [.. sorted]);
    }

    public static BakedBehaviorsInfo Bake(params BehaviorsInfo[] layers)
    {
        // 合并声明翻译器
        var mergedTranslatorTypes = layers
            .SelectMany(l => l.DeclarationTranslatorTypes)
            .ToImmutableDictionary();

        // 合并概念
        var conceptInfos = new Dictionary<string, ConceptInfo>();
        foreach (var layer in layers)
        {
            foreach (var (key, relatedTypes) in layer.ConceptTypes)
            {
                if (conceptInfos.TryGetValue(key, out var conceptInfo))
                {
                    if (relatedTypes.DescriptionType is not null)
                        throw new Exception("Concept description cannot be extended!");
                    var extendedConcept = conceptInfo.Extend(
                        relatedTypes.DefinitionTypes.FirstOrDefault(),
                        relatedTypes.ApplierTypes.FirstOrDefault()
                    );
                    conceptInfos[key] = extendedConcept;
                }
                else
                {
                    if (relatedTypes.DefinitionTypes.IsEmpty)
                        throw new Exception("A new concept must be provided a definition!");
                    var newConcept = ConceptInfo.Define(
                        key,
                        relatedTypes.DefinitionTypes[0],
                        relatedTypes.DescriptionType,
                        relatedTypes.ApplierTypes.FirstOrDefault()
                    );
                    conceptInfos.Add(key, newConcept);
                }
            }
        }
        var mergedConceptInfos = conceptInfos.ToImmutableDictionary();

        // 合并系统类型。合并后完成拓扑排序
        var mergedSystemTypes = new ImmutableSortedSystemTypeCollection(
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Input).ToImmutableHashSet()),
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Ai).ToImmutableHashSet()),
            BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Simulate).ToImmutableHashSet()
            ),
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Render).ToImmutableHashSet())
        );

        // 合并钩子函数
        var mergedImplMethods = layers
            .SelectMany(l => l.HookImplMethods)
            .SelectMany(kv => kv.Value, (kv, i) => (kv.Key, Info: i))
            .GroupBy(p => p.Key)
            .ToImmutableDictionary(g => g.Key, g => g.Select(p => p.Info).ToImmutableArray());

        return new BakedBehaviorsInfo(
            mergedTranslatorTypes,
            mergedConceptInfos,
            mergedSystemTypes,
            mergedImplMethods
        );
    }
}
