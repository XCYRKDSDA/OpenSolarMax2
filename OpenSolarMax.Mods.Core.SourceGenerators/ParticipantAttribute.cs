using Microsoft.CodeAnalysis;

namespace OpenSolarMax.Mods.Core.SourceGenerators;

/// <summary>
/// 
/// </summary>
/// <param name="exclusive">该成员是否只能参与一个关系</param>
/// <param name="theOther">在该成员的索引组件中，使用关系中的哪个成员协助索引</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ParticipantAttribute(
    bool exclusive = true, string? theOther = null
) : Attribute
{
    public bool Exclusive => exclusive;
    public string? TheOther => theOther;

    public static ParticipantAttribute? FromAttributeData(AttributeData data)
    {
        bool exclusive = true;
        string? theOther = null;

        for (int i = 0; i < data.ConstructorArguments.Length; i++)
        {
            var arg = data.ConstructorArguments[i];
            if (arg.Kind == TypedConstantKind.Error)
                return null;

            switch (i)
            {
                case 0: exclusive = (bool)arg.Value!; break;
                case 1: theOther = (string?)arg.Value; break;
                default: return null;
            }
        }

        foreach (var (name, arg) in data.NamedArguments)
        {
            if (arg.Kind == TypedConstantKind.Error)
                return null;

            switch (name)
            {
                case "exclusive": exclusive = (bool)arg.Value!; break;
                case "theOther": theOther = (string?)arg.Value; break;
                default: return null;
            }
        }

        return new ParticipantAttribute(exclusive, theOther);
    }
}
