using Arch.Buffer;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class RelativeTransformTemplate : ITemplate
{
    #region Configurations

    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }

    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    #endregion

    private static readonly Signature _signature = new(
        typeof(TreeRelationship<RelativeTransform>),
        typeof(RelativeTransform)
    );

    public Signature Signature => _signature;

    public void Apply(Entity entity)
    {
        entity.Set(new TreeRelationship<RelativeTransform>(Parent, Child));
        entity.Set(new RelativeTransform(Translation, Rotation));
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new TreeRelationship<RelativeTransform>(Parent, Child));
        commandBuffer.Set(in entity, new RelativeTransform(Translation, Rotation));
    }

    public object Clone() => MemberwiseClone();
}
