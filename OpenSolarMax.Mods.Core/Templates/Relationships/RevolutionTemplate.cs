using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class RevolutionTemplate : ITemplate
{
    #region Configurations

    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }

    public required Vector2 Shape { get; set; }

    public required float Period { get; set; }

    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public float InitPhase { get; set; } = 0;

    #endregion

    private static readonly Signature _signature = new(
        typeof(TreeRelationship<RelativeTransform>),
        typeof(RelativeTransform),
        typeof(RevolutionOrbit),
        typeof(RevolutionState)
    );

    public Signature Signature => _signature;

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new TreeRelationship<RelativeTransform>(Parent, Child));
        commandBuffer.Set(in entity, new RevolutionOrbit { Shape = Shape, Period = Period, Rotation = Rotation });
        commandBuffer.Set(in entity, new RevolutionState { Phase = InitPhase });
    }
}
