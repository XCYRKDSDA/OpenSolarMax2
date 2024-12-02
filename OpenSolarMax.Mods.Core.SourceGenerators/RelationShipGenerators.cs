using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenSolarMax.Mods.Core.SourceGenerators;

internal record ParticipantInfo(string Type, string Member);

internal record RelationshipInfo(
    string Namespace, string Symbol, string Type,
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

        namespace <<NAMESPACE>>;

        partial <<RELATIONSHIP_SYMBOL>> <<RELATIONSHIP_TYPE>>() : IRelationshipRecord
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

    private const string _participantTemplate =
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

    public void Initialize(GeneratorInitializationContext context) { }

    private static void GenerateRelationship(RelationshipInfo info, GeneratorExecutionContext context)
    {
        var participantsTypes = string.Join(
            ", ", info.Participants.Select(p => $"typeof({p.Type})")
        );

        var participantsCount = info.Participants.Length;

        var indexerBody = string.Join(
            "\n            else ", info.Participants.Select(
                p => $"if (key == typeof({p.Type})) yield return {p.Member};")
        );

        var containsExpression = string.Join(
            " || ", info.Participants.Select(
                p => $"key == typeof({p.Type})")
        );

        var enumeratorBody = string.Join(
            "\n        ",
            info.Participants.Select(
                p => $"yield return new SingleItemGroup<Type, EntityReference>(typeof({p.Type}), {p.Member});")
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
        context.AddSource($"{info.Type}.g.cs", relationshipCs);

        foreach (var participant in info.Participants)
        {
            var participantsCs =
                _participantTemplate.Replace("<<NAMESPACE>>", info.Namespace)
                                    .Replace("<<RELATIONSHIP_SYMBOL>>", info.Symbol)
                                    .Replace("<<RELATIONSHIP_TYPE>>", info.Type)
                                    .Replace("<<PARTICIPANT_TYPE>>", participant.Type);
            context.AddSource($"{info.Type}.{participant.Type}.g.cs", participantsCs);
        }
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
                    Type: namedTypeSymbol.Name,
                    Participants: members.Select(m => new ParticipantInfo($"As{m.Name}", $"{m.Name}")).ToArray()
                );

                GenerateRelationship(info, context);
            }
        }
    }
}
