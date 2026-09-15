using Microsoft.CodeAnalysis;

namespace OpenSolarMax.Mods.Common.SourceGenerators;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RelationshipAttribute() : Attribute
{
    public static RelationshipAttribute? FromAttributeData(AttributeData data)
    {
        return new RelationshipAttribute();
    }
}
