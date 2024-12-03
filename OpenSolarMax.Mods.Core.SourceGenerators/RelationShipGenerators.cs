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
    private const string _relationshipTemplate =
        """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using Arch.Core;
        using OpenSolarMax.Mods.Core.Components;
        using OpenSolarMax.Mods.Core.Utils;

        namespace <<NAMESPACE>>;

        partial <<RELATIONSHIP_SYMBOL>> <<RELATIONSHIP_TYPE>> : IRelationshipRecord
        {
            static Type[] IRelationshipRecord.ParticipantTypes => [<<PARTICIPANTS_TYPES>>];
        
            readonly int ILookup<Type, EntityReference>.Count => <<PARTICIPANTS_COUNT>>;
        
            readonly IEnumerable<EntityReference> ILookup<Type, EntityReference>.this[Type key]
            {
                get
                {
                    <<INDEXER_BODY>>
                }
            }
        
            readonly bool ILookup<Type, EntityReference>.Contains(Type key)
            {
                return <<CONTAINS_EXPRESSION>>;
            }
            
            readonly IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
            {
                <<ENUMERATOR_BODY>>
            }
            
            readonly IEnumerator IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();
            }
        }
                
        """;

    private const string _participant1Template =
        """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using Arch.Core;
        using OpenSolarMax.Mods.Core.Components;
        using OpenSolarMax.Mods.Core.Utils;

        namespace <<NAMESPACE>>;

        partial <<RELATIONSHIP_SYMBOL>> <<RELATIONSHIP_TYPE>>
        {
            public struct <<PARTICIPANT_TYPE>>(): IParticipantIndex
            {
                public EntityReference Relationship = EntityReference.Null;
                
                #region IParticipantIndex
                
                readonly int ICollection<EntityReference>.Count => 1;
        
                readonly bool ICollection<EntityReference>.IsReadOnly => false;
                
                readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
                {
                    array[arrayIndex] = Relationship;
                }
                
                readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
                {
                    yield return Relationship;
                }
                
                readonly IEnumerator IEnumerable.GetEnumerator()
                {
                    return (this as IEnumerable<EntityReference>).GetEnumerator();
                }
                
                readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
                {
                    return Relationship == relationship;
                }
                
                void ICollection<EntityReference>.Add(EntityReference relationship)
                {
                    if (Relationship != EntityReference.Null)
                        throw new IndexOutOfRangeException();
                    Relationship = relationship;
                }
                
                bool ICollection<EntityReference>.Remove(EntityReference relationship)
                {
                    if (Relationship != relationship)
                        return false;
                
                    Relationship = EntityReference.Null;
                    return true;
                }
                
                void ICollection<EntityReference>.Clear()
                {
                    Relationship = EntityReference.Null;
                }
                
                #endregion
            }
        }
        """;

    private const string _participant2Template =
        """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using Arch.Core;
        using OpenSolarMax.Mods.Core.Components;
        using OpenSolarMax.Mods.Core.Utils;

        namespace <<NAMESPACE>>;

        partial <<RELATIONSHIP_SYMBOL>> <<RELATIONSHIP_TYPE>>
        {
            public struct <<PARTICIPANT_TYPE>>(): IParticipantIndex
            {
                public HashSet<EntityReference> Relationships = [];
                
                #region IParticipantIndex
                
                readonly int ICollection<EntityReference>.Count => Relationships.Count;
        
                readonly bool ICollection<EntityReference>.IsReadOnly => false;
                
                readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
                {
                    Relationships.CopyTo(array, arrayIndex);
                }
                
                readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
                {
                    return Relationships.GetEnumerator();
                }
                
                readonly IEnumerator IEnumerable.GetEnumerator()
                {
                    return (this as IEnumerable<EntityReference>).GetEnumerator();
                }
                
                readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
                {
                    return Relationships.Contains(relationship);
                }
                
                void ICollection<EntityReference>.Add(EntityReference relationship)
                {
                    Relationships.Add(relationship);
                }
                
                bool ICollection<EntityReference>.Remove(EntityReference relationship)
                {
                    return Relationships.Remove(relationship);
                }
                
                void ICollection<EntityReference>.Clear()
                {
                    Relationships.Clear();
                }
                
                #endregion
            }
        }
        """;

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
                p => p.Multiple ? $"({p.Member} as IEnumerable<EntityReference>).Count()" : "1")
        );

        var indexerBody = string.Join(
            "\n            else ",
            info.Participants.Select(
                    p => p.Multiple
                             ? $"if (key == typeof({p.Type})) return {p.Member};"
                             : $"if (key == typeof({p.Type})) return Enumerable.Repeat({p.Member}, 1);")
                .Append("return Enumerable.Empty<EntityReference>();")
        );

        var containsExpression = string.Join(
            " || ",
            info.Participants.Select(
                p => p.Multiple
                         ? $"(key == typeof({p.Type}) && ({p.Member} as IEnumerable<EntityReference>).Count() != 0)"
                         : $"key == typeof({p.Type})")
        );

        var enumeratorBody = string.Join(
            "\n        ",
            info.Participants.Select(
                p => p.Multiple
                         ? $"yield return new EnumerableGroup<Type, EntityReference>(typeof({p.Type}), {p.Member});"
                         : $"yield return new SingleItemGroup<Type, EntityReference>(typeof({p.Type}), {p.Member});")
        );

        var relationshipCs =
            _relationshipTemplate.Replace("<<NAMESPACE>>", info.Namespace)
                                 .Replace("<<RELATIONSHIP_SYMBOL>>", info.Symbol)
                                 .Replace("<<RELATIONSHIP_TYPE>>", info.Type)
                                 .Replace("<<PARTICIPANTS_TYPES>>", participantsTypes)
                                 .Replace("<<PARTICIPANTS_COUNT>>", participantsCount.ToString())
                                 .Replace("<<INDEXER_BODY>>", indexerBody)
                                 .Replace("<<CONTAINS_EXPRESSION>>", containsExpression)
                                 .Replace("<<ENUMERATOR_BODY>>", enumeratorBody);
        context.AddSource($"{fullname}.g.cs", relationshipCs);

        foreach (var participant in info.Participants)
        {
            var template = participant.Attr.Exclusive ? _participant1Template : _participant2Template;
            var participantsCs =
                template.Replace("<<NAMESPACE>>", info.Namespace)
                        .Replace("<<RELATIONSHIP_SYMBOL>>", info.Symbol)
                        .Replace("<<RELATIONSHIP_TYPE>>", info.Type)
                        .Replace("<<PARTICIPANT_TYPE>>", participant.Type);
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
        var entityReferenceSymbol = compilation.GetTypeByMetadataName("Arch.Core.EntityReference")!;

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
