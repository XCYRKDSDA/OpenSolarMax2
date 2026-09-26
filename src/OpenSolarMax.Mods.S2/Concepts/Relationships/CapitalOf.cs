using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string CapitalOf = "CapitalOf";
}

[Define(ConceptNames.CapitalOf)]
public abstract class CapitalOfDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(CapitalOf));
}

[Describe(ConceptNames.CapitalOf)]
public class CapitalOfDescription : IDescription
{
    /// <summary>
    /// 首府天体
    /// </summary>
    public required Entity Capital { get; set; }

    /// <summary>
    /// 首府所属的阵营
    /// </summary>
    public required Entity Team { get; set; }
}

[Apply(ConceptNames.CapitalOf)]
public class CapitalOfApplier : IApplier<CapitalOfDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, CapitalOfDescription desc)
    {
        commandBuffer.Set(in entity, new CapitalOf(desc.Capital, desc.Team));
    }
}
