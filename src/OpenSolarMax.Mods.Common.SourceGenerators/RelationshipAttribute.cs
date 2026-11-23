using Microsoft.CodeAnalysis;

namespace OpenSolarMax.Mods.Common.SourceGenerators;

/// <summary>
/// 生成概念时所属的场景
/// </summary>
[Flags]
public enum ConceptScenes
{
    Gameplay = 1 << 0,
    Preview = 1 << 1,
    Both = Gameplay | Preview,
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RelationshipAttribute(
    bool generateConcept = false,
    ConceptScenes scenes = ConceptScenes.Gameplay
) : Attribute
{
    /// <summary>
    /// 是否为该关系同时生成同名的概念
    /// </summary>
    public bool GenerateConcept => generateConcept;

    /// <summary>
    /// 生成的概念所属的场景
    /// </summary>
    public ConceptScenes Scenes => scenes;

    public static RelationshipAttribute? FromAttributeData(AttributeData data)
    {
        var generateConcept = false;
        var scenes = ConceptScenes.Gameplay;

        for (int i = 0; i < data.ConstructorArguments.Length; i++)
        {
            var arg = data.ConstructorArguments[i];
            if (arg.Kind == TypedConstantKind.Error)
                return null;

            switch (i)
            {
                case 0:
                    generateConcept = (bool)arg.Value!;
                    break;
                case 1:
                    scenes = (ConceptScenes)(int)arg.Value!;
                    break;
                default:
                    return null;
            }
        }

        foreach (var (name, arg) in data.NamedArguments)
        {
            if (arg.Kind == TypedConstantKind.Error)
                return null;

            switch (name)
            {
                case "generateConcept":
                    generateConcept = (bool)arg.Value!;
                    break;
                case "scenes":
                    scenes = (ConceptScenes)(int)arg.Value!;
                    break;
                default:
                    return null;
            }
        }

        return new RelationshipAttribute(generateConcept, scenes);
    }
}
