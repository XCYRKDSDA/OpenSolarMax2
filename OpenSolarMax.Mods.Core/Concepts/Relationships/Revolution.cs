using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Revolution = "Revolution";
}

[Define(ConceptNames.Revolution)]
public abstract class RevolutionDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(TreeRelationship<RelativeTransform>),
        typeof(RelativeTransform),
        typeof(RevolutionOrbit),
        typeof(RevolutionState)
    );
}

[Describe(ConceptNames.Revolution)]
public class RevolutionDescription : IDescription
{
    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }

    public required Vector2 Shape { get; set; }

    public required float Period { get; set; }

    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public float InitPhase { get; set; } = 0;
}

[Apply(ConceptNames.Revolution)]
public class RevolutionApplier : IApplier<RevolutionDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, RevolutionDescription desc)
    {
        commandBuffer.Set(in entity, new TreeRelationship<RelativeTransform>(desc.Parent, desc.Child));
        commandBuffer.Set(
            in entity, new RevolutionOrbit { Shape = desc.Shape, Period = desc.Period, Rotation = desc.Rotation });
        commandBuffer.Set(in entity, new RevolutionState { Phase = desc.InitPhase });
    }
}
