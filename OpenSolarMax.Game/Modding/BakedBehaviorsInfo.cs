using System.Collections.Immutable;
using System.Reflection;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

internal record BakedBehaviorsInfo(
    ImmutableDictionary<string, DeclarationTranslatorInfo> TranslatorTypes,
    ImmutableDictionary<string, ConceptInfo> ConceptInfos,
    ImmutableSortedSystemTypeCollection SystemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods
)
{
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
                    if (relatedTypes.Description is not null)
                        throw new Exception("Concept description cannot be extended!");
                    var extendedConcept = conceptInfo.Extend(
                        relatedTypes.Definition,
                        relatedTypes.Applier
                    );
                    conceptInfos[key] = extendedConcept;
                }
                else
                {
                    if (relatedTypes.Definition is null)
                        throw new Exception("A new concept must be provided a definition!");
                    var newConcept = ConceptInfo.Define(
                        key,
                        relatedTypes.Definition,
                        relatedTypes.Description,
                        relatedTypes.Applier
                    );
                    conceptInfos.Add(key, newConcept);
                }
            }
        }
        var mergedConceptInfos = conceptInfos.ToImmutableDictionary();

        // 合并引导系统类型并排序
        var mergedBootstrapSystemTypes = layers
            .SelectMany(l => l.SystemTypes.Bootstrap)
            .ToImmutableHashSet();
        var bootstrapOrders = SystemsTopology.ExtractBootstrapOrders(mergedBootstrapSystemTypes);
        var sortedBootstrapSystems = SystemsTopology.TopologicalSortSystems(
            mergedBootstrapSystemTypes,
            bootstrapOrders
        );
        var bootstrapSortedSystemTypes = new ImmutableSortedSystemTypes(
            mergedBootstrapSystemTypes,
            [.. bootstrapOrders],
            [.. sortedBootstrapSystems]
        );

        // 合并系统类型。合并后完成拓扑排序
        var mergedSystemTypes = new ImmutableSortedSystemTypeCollection(
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Input).ToImmutableHashSet()),
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Ai).ToImmutableHashSet()),
            BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Simulate).ToImmutableHashSet()
            ),
            BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Render).ToImmutableHashSet()
            ),
            bootstrapSortedSystemTypes
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
