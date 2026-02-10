using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string RelativeTransform = "RelativeTransform";
}

[Define(ConceptNames.RelativeTransform)]
public abstract class RelativeTransformDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(TreeRelationship<RelativeTransform>),
        typeof(RelativeTransform)
    );
}

[Describe(ConceptNames.RelativeTransform)]
public class RelativeTransformDescription : IDescription
{
    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }

    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}

[Apply(ConceptNames.RelativeTransform)]
public class RelativeTransformApplier : IApplier<RelativeTransformDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, RelativeTransformDescription desc)
    {
        commandBuffer.Set(in entity, new TreeRelationship<RelativeTransform>(desc.Parent, desc.Child));
        commandBuffer.Set(in entity, new RelativeTransform(desc.Translation, desc.Rotation));
    }
}
