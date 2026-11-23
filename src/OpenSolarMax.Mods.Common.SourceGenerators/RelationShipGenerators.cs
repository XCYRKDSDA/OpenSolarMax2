using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenSolarMax.Mods.Common.SourceGenerators;

internal record ParticipantInfo(
    string Type,
    string Member,
    bool Multiple,
    ParticipantAttribute Attr
);

internal record RelationshipInfo(
    string Namespace,
    string Symbol,
    string Type,
    RelationshipAttribute Attr,
    ParticipantInfo[] Participants
);

[Generator]
public class RelationShipGenerator : ISourceGenerator
{
    private static readonly string _relationshipTemplate = ResourceHelper.GetEmbeddedText(
        "Templates/Relationship.in"
    );

    private static readonly string _participant1Template = ResourceHelper.GetEmbeddedText(
        "Templates/ExclusiveParticipant.in"
    );

    private static readonly string _participant2Template = ResourceHelper.GetEmbeddedText(
        "Templates/NonExclusiveParticipant.in"
    );

    private static readonly string _conceptTemplate = ResourceHelper.GetEmbeddedText(
        "Templates/RelationshipConcept.in"
    );

    public void Initialize(GeneratorInitializationContext context) { }

    private static void GenerateRelationship(
        string fullname,
        INamedTypeSymbol typeSymbol,
        RelationshipInfo info,
        GeneratorExecutionContext context
    )
    {
        var componentsNamespace = context
            .Compilation.GetTypeByMetadataName(
                "OpenSolarMax.Mods.Common.Components.IRelationshipRecord"
            )!
            .ContainingNamespace.ToString();

        var utilsNamespace = context
            .Compilation.GetTypeByMetadataName("OpenSolarMax.Mods.Common.Utils.SingleItemGroup`2")!
            .ContainingNamespace.ToString();

        var participantsTypes = string.Join(
            ", ",
            info.Participants.Select(p => $"typeof({p.Type})")
        );

        var participantsCount = string.Join(
            " + ",
            info.Participants.Select(p =>
                p.Multiple ? $"({p.Member} as IEnumerable<Entity>).Count()" : "1"
            )
        );

        var indexerBody = string.Join(
            "\n            else ",
            info.Participants.Select(p =>
                    p.Multiple
                        ? $"if (key == typeof({p.Type})) return {p.Member};"
                        : $"if (key == typeof({p.Type})) return Enumerable.Repeat({p.Member}, 1);"
                )
                .Append("return Enumerable.Empty<Entity>();")
        );

        var containsExpression = string.Join(
            " || ",
            info.Participants.Select(p =>
                p.Multiple
                    ? $"(key == typeof({p.Type}) && ({p.Member} as IEnumerable<Entity>).Count() != 0)"
                    : $"key == typeof({p.Type})"
            )
        );

        var enumeratorBody = string.Join(
            "\n        ",
            info.Participants.Select(p =>
                p.Multiple
                    ? $"yield return new EnumerableGroup<Type, Entity>(typeof({p.Type}), {p.Member});"
                    : $"yield return new SingleItemGroup<Type, Entity>(typeof({p.Type}), {p.Member});"
            )
        );

        var relationshipCs = _relationshipTemplate
            .Replace("@COMPONENTS_NAMESPACE@", componentsNamespace)
            .Replace("@UTILS_NAMESPACE@", utilsNamespace)
            .Replace("@NAMESPACE@", info.Namespace)
            .Replace("@RELATIONSHIP_SYMBOL@", info.Symbol)
            .Replace("@RELATIONSHIP_TYPE@", info.Type)
            .Replace("@PARTICIPANTS_TYPES@", participantsTypes)
            .Replace("@PARTICIPANTS_COUNT@", participantsCount.ToString())
            .Replace("@INDEXER_BODY@", indexerBody)
            .Replace("@CONTAINS_EXPRESSION@", containsExpression)
            .Replace("@ENUMERATOR_BODY@", enumeratorBody);
        context.AddSource($"{fullname}.g.cs", relationshipCs);

        foreach (var participant in info.Participants)
        {
            var template = participant.Attr.Exclusive
                ? _participant1Template
                : _participant2Template;
            var participantsCs = template
                .Replace("@COMPONENTS_NAMESPACE@", componentsNamespace)
                .Replace("@UTILS_NAMESPACE@", utilsNamespace)
                .Replace("@NAMESPACE@", info.Namespace)
                .Replace("@RELATIONSHIP_SYMBOL@", info.Symbol)
                .Replace("@RELATIONSHIP_TYPE@", info.Type)
                .Replace("@PARTICIPANT_TYPE@", participant.Type);
            context.AddSource($"{fullname}.{participant.Type}.g.cs", participantsCs);
        }

        if (info.Attr.GenerateConcept)
            GenerateRelationshipConcept(fullname, typeSymbol, info, context);
    }

    private static void GenerateRelationshipConcept(
        string fullname,
        INamedTypeSymbol typeSymbol,
        RelationshipInfo info,
        GeneratorExecutionContext context
    )
    {
        // 泛型关系的概念名和描述字段由每个具体概念自行决定，不能自动生成
        if (typeSymbol.IsGenericType)
            return;

        var constructor = typeSymbol
            .InstanceConstructors.Where(c => c.Parameters.Length > 0)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();
        if (constructor is null)
            return;

        var members = typeSymbol
            .GetMembers()
            .Where(m =>
                m is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false }
            )
            .ToList();

        var properties = new List<string>();
        var arguments = new List<string>();

        foreach (var parameter in constructor.Parameters)
        {
            var member = members.FirstOrDefault(m =>
                string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)
            );
            var propertyName =
                member?.Name
                ?? char.ToUpperInvariant(parameter.Name[0]) + parameter.Name.Substring(1);
            var propertyType = parameter.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );

            properties.Add($"    public required {propertyType} {propertyName} {{ get; set; }}");
            arguments.Add($"desc.{propertyName}");
        }

        var sceneAttribute =
            (info.Attr.Scenes & ConceptScenes.Preview) != 0
                ? (info.Attr.Scenes & ConceptScenes.Gameplay) != 0
                    ? "[global::OpenSolarMax.Game.Modding.BothForGameplayAndPreview]\n"
                    : "[global::OpenSolarMax.Game.Modding.OnlyForPreview]\n"
                : string.Empty;

        var conceptCs = _conceptTemplate
            .Replace("@NAMESPACE@", $"{context.Compilation.AssemblyName}.Concepts")
            .Replace("@NAME@", typeSymbol.Name)
            .Replace("@SCENE_ATTRIBUTE@", sceneAttribute)
            .Replace(
                "@RELATIONSHIP_TYPE@",
                typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            )
            .Replace("@PROPERTIES@", string.Join("\n\n", properties))
            .Replace("@ARGUMENTS@", string.Join(", ", arguments));

        context.AddSource($"{fullname}.Concept.g.cs", conceptCs);
    }

    private static bool IsMultiple(ISymbol memberSymbol, Compilation compilation)
    {
        var typeSymbol = memberSymbol switch
        {
            IFieldSymbol fieldSymbol => fieldSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => throw new ArgumentOutOfRangeException(nameof(memberSymbol)),
        };

        var genericIEnumerableSymbol = compilation.GetTypeByMetadataName(
            "System.Collections.Generic.IEnumerable`1"
        )!;
        var entityReferenceSymbol = compilation.GetTypeByMetadataName("Arch.Core.Entity")!;

        return typeSymbol.AllInterfaces.Any(i =>
            i.OriginalDefinition.Equals(genericIEnumerableSymbol, SymbolEqualityComparer.Default)
            && i.TypeArguments.Length == 1
            && i.TypeArguments[0].Equals(entityReferenceSymbol, SymbolEqualityComparer.Default)
        );
    }

    public void Execute(GeneratorExecutionContext context)
    {
        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();
            foreach (var typeDeclaration in typeDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration);
                if (
                    symbol is not INamedTypeSymbol namedTypeSymbol
                    || !symbol
                        .GetAttributes()
                        .Any(a => a.AttributeClass?.Name == nameof(RelationshipAttribute))
                )
                    continue;

                var members = namedTypeSymbol
                    .GetMembers()
                    .Where(m => m is IFieldSymbol or IPropertySymbol)
                    .Where(m =>
                        m.GetAttributes()
                            .Any(a => a.AttributeClass?.Name == nameof(ParticipantAttribute))
                    )
                    .ToList();

                if (members.Count == 0)
                    continue;

                var info = new RelationshipInfo(
                    Namespace: namedTypeSymbol.ContainingNamespace.ToString()!,
                    Symbol: namedTypeSymbol.TypeKind switch
                    {
                        TypeKind.Class => "class",
                        TypeKind.Struct => "struct",
                        _ => throw new Exception(),
                    },
                    Type: namedTypeSymbol.ToDisplayString(
                        SymbolDisplayFormat.MinimallyQualifiedFormat
                    ),
                    Attr: RelationshipAttribute.FromAttributeData(
                        namedTypeSymbol
                            .GetAttributes()
                            .First(a => a.AttributeClass?.Name == nameof(RelationshipAttribute))
                    )!,
                    Participants: members
                        .Select(m => new ParticipantInfo(
                            Type: $"As{m.Name}",
                            Member: $"{m.Name}",
                            Multiple: IsMultiple(m, context.Compilation),
                            Attr: ParticipantAttribute.FromAttributeData(
                                m.GetAttributes()
                                    .First(a =>
                                        a.AttributeClass?.Name == nameof(ParticipantAttribute)
                                    )
                            )!
                        ))
                        .ToArray()
                );

                var fullnameStyle = new SymbolDisplayFormat(
                    genericsOptions: SymbolDisplayGenericsOptions.None,
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
                );
                GenerateRelationship(
                    namedTypeSymbol.ToDisplayString(fullnameStyle),
                    namedTypeSymbol,
                    info,
                    context
                );
            }
        }
    }
}
