using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenSolarMax.Mods.Core.SourceGenerators;

internal record ParticipantInfo(string Type, string Member, bool Multiple, ParticipantAttribute Attr);

internal record RelationshipInfo(
    string Namespace, string Symbol, string Type, RelationshipAttribute Attr,
    ParticipantInfo[] Participants);

[Generator]
public class RelationShipGenerator : ISourceGenerator
{
    private static readonly string _relationshipTemplate =
        ResourceHelper.GetEmbeddedText("Templates/Relationship.in");

    private static readonly string _participant1Template =
        ResourceHelper.GetEmbeddedText("Templates/ExclusiveParticipant.in");

    private static readonly string _participant2Template =
        ResourceHelper.GetEmbeddedText("Templates/NonExclusiveParticipant.in");

    public void Initialize(GeneratorInitializationContext context) { }

    private static void GenerateRelationship(string fullname, RelationshipInfo info, GeneratorExecutionContext context)
    {
        var participantsTypes = string.Join(
            ", ",
            info.Participants.Select(p => $"typeof({p.Type})")
        );

        var participantsCount = string.Join(
            " + ",
            info.Participants.Select(
                p => p.Multiple ? $"({p.Member} as IEnumerable<Entity>).Count()" : "1")
        );

        var indexerBody = string.Join(
            "\n            else ",
            info.Participants.Select(
                    p => p.Multiple
                             ? $"if (key == typeof({p.Type})) return {p.Member};"
                             : $"if (key == typeof({p.Type})) return Enumerable.Repeat({p.Member}, 1);")
                .Append("return Enumerable.Empty<Entity>();")
        );

        var containsExpression = string.Join(
            " || ",
            info.Participants.Select(
                p => p.Multiple
                         ? $"(key == typeof({p.Type}) && ({p.Member} as IEnumerable<Entity>).Count() != 0)"
                         : $"key == typeof({p.Type})")
        );

        var enumeratorBody = string.Join(
            "\n        ",
            info.Participants.Select(
                p => p.Multiple
                         ? $"yield return new EnumerableGroup<Type, Entity>(typeof({p.Type}), {p.Member});"
                         : $"yield return new SingleItemGroup<Type, Entity>(typeof({p.Type}), {p.Member});")
        );

        var relationshipCs =
            _relationshipTemplate.Replace("@NAMESPACE@", info.Namespace)
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
            var template = participant.Attr.Exclusive ? _participant1Template : _participant2Template;
            var participantsCs =
                template.Replace("@NAMESPACE@", info.Namespace)
                        .Replace("@RELATIONSHIP_SYMBOL@", info.Symbol)
                        .Replace("@RELATIONSHIP_TYPE@", info.Type)
                        .Replace("@PARTICIPANT_TYPE@", participant.Type);
            context.AddSource($"{fullname}.{participant.Type}.g.cs", participantsCs);
        }
    }

    private static bool IsMultiple(ISymbol memberSymbol, Compilation compilation)
    {
        var typeSymbol = memberSymbol switch
        {
            IFieldSymbol fieldSymbol => fieldSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => throw new ArgumentOutOfRangeException(nameof(memberSymbol))
        };

        var genericIEnumerableSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!;
        var entityReferenceSymbol = compilation.GetTypeByMetadataName("Arch.Core.Entity")!;

        return typeSymbol.AllInterfaces.Any(
            i => i.OriginalDefinition.Equals(genericIEnumerableSymbol, SymbolEqualityComparer.Default)
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
                if (symbol is not INamedTypeSymbol namedTypeSymbol
                    || !symbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(RelationshipAttribute)))
                    continue;

                var members =
                    namedTypeSymbol
                        .GetMembers()
                        .Where(m => m is IFieldSymbol or IPropertySymbol)
                        .Where(m => m.GetAttributes()
                                     .Any(a => a.AttributeClass?.Name == nameof(ParticipantAttribute)))
                        .ToList();

                if (members.Count == 0)
                    continue;

                var info = new RelationshipInfo
                (
                    Namespace: namedTypeSymbol.ContainingNamespace.ToString()!,
                    Symbol: namedTypeSymbol.TypeKind switch
                    {
                        TypeKind.Class => "class",
                        TypeKind.Struct => "struct",
                        _ => throw new Exception()
                    },
                    Type: namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    Attr: RelationshipAttribute.FromAttributeData(
                        namedTypeSymbol.GetAttributes()
                                       .First(a => a.AttributeClass?.Name ==
                                                   nameof(RelationshipAttribute))
                    )!,
                    Participants: members
                                  .Select(m => new ParticipantInfo(
                                              Type: $"As{m.Name}",
                                              Member: $"{m.Name}",
                                              Multiple: IsMultiple(m, context.Compilation),
                                              Attr: ParticipantAttribute.FromAttributeData(
                                                  m.GetAttributes()
                                                   .First(a => a.AttributeClass?.Name == nameof(ParticipantAttribute))
                                              )!
                                          ))
                                  .ToArray()
                );

                var fullnameStyle = new SymbolDisplayFormat(
                    genericsOptions: SymbolDisplayGenericsOptions.None,
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                GenerateRelationship(namedTypeSymbol.ToDisplayString(fullnameStyle),
                                     info, context);
            }
        }
    }
}
