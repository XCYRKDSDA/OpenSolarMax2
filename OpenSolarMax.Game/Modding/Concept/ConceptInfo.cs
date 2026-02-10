using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.Concept;

/// <summary>
/// 概念的定义，记录了一个概念的所有定义、描述和应用器
/// </summary>
public record ConceptInfo
{
    public string Name { get; }

    public ImmutableArray<Type> DefinitionTypes { get; }

    public Type? DescriptionType { get; }

    public ImmutableArray<Type> ApplierTypes { get; }

    private ConceptInfo(string name, IReadOnlyList<Type> definitionTypes, Type? descriptionType,
                        IReadOnlyList<Type> applierTypes)
    {
        Name = name;
        DefinitionTypes = [..definitionTypes];
        DescriptionType = descriptionType;
        ApplierTypes = [..applierTypes];
    }

    public static ConceptInfo Define(string name, Type definitionType, Type? descriptionType, Type? applierType)
    {
        var expectedApplierInterface =
            descriptionType is null ? typeof(IApplier) : typeof(IApplier<>).MakeGenericType(descriptionType);
        if (applierType is not null && !applierType.GetInterfaces().Contains(expectedApplierInterface))
            throw new Exception("the applier type must support the corresponding definition type!");

        return new ConceptInfo(name, [definitionType], descriptionType, applierType is null ? [] : [applierType]);
    }

    public ConceptInfo Extend(Type? definitionType, Type? applierType)
    {
        var expectedApplierInterface =
            DescriptionType is null ? typeof(IApplier) : typeof(IApplier<>).MakeGenericType(DescriptionType);
        if (applierType is not null && !applierType.GetInterfaces().Contains(expectedApplierInterface))
            throw new Exception("the applier type must support the corresponding definition type!");

        return new ConceptInfo(Name, definitionType is null ? DefinitionTypes : [..DefinitionTypes, definitionType],
                               DescriptionType, applierType is null ? ApplierTypes : [..ApplierTypes, applierType]);
    }
}
