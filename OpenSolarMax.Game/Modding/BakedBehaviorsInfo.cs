using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Modding;

internal record BakedBehaviorsInfo(
    ImmutableDictionary<string, DeclarationTranslatorInfo> TranslatorTypes,
    ImmutableDictionary<string, ConceptInfo> ConceptInfos,
    StageSystemTypesCollection SystemTypes,
    ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods
)
{
    private static ImmutableSortedSystemTypesCollection BakeSortedSystemTypes(
        IReadOnlySet<Type> systemTypes
    )
    {
        var declarations = SystemsTopology.ExtractExecutionOrders(systemTypes);
        var graphs = SystemsTopology.ComposeExecutionGraph(declarations);
        Debug.WriteLine("=== DOT GRAPH (for programmatic parsing) ===");
        Debug.WriteLine(SystemsTopology.BuildSystemTopologyDotGraph(declarations, graphs));
        Debug.WriteLine("=== D2 GRAPH (for visualization) ===");
        Debug.WriteLine(SystemsTopology.BuildSystemTopologyD2Graph(declarations, graphs));

        return new ImmutableSortedSystemTypesCollection(
            UpdateSystems: SystemsTopology.TopologicalSortSystems(graphs.Update),
            LateUpdate1Systems: SystemsTopology.TopologicalSortSystems(graphs.LateUpdate1),
            LateUpdate2Systems: SystemsTopology.TopologicalSortSystems(graphs.LateUpdate2)
        );
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

        // 合并系统类型。合并后完成拓扑排序
        var mergedSystemTypes = new StageSystemTypesCollection(
            Input: BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Input).ToImmutableHashSet()
            ),
            Ai: BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Ai).ToImmutableHashSet()
            ),
            Simulate: BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Simulate).ToImmutableHashSet()
            ),
            Render: BakeSortedSystemTypes(
                layers.SelectMany(l => l.SystemTypes.Render).ToImmutableHashSet()
            )
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
