using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Templates;

public class PredefinedOrbitTemplate : ITemplate, ITransformableTemplate
{
    #region Options

    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; }

    /// <summary>
    /// 轨道的形状
    /// </summary>
    public required Vector2 Shape { get; set; }

    /// <summary>
    /// 轨道的公转周期
    /// </summary>
    public required float Period { get; set; }

    /// <summary>
    /// 轨道的偏转
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    #endregion

    private static readonly Signature _signature = new(
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        typeof(PredefinedOrbit)
    );

    public Signature Signature => _signature;

    public void Apply(Entity entity)
    {
        (this as ITransformableTemplate).Apply(entity);

        ref var orbit = ref entity.Get<PredefinedOrbit>();
        orbit.Template.Shape = new(Shape.X, Shape.Y);
        orbit.Template.Period = Period;
        orbit.Template.Rotation = Rotation;
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        (this as ITransformableTemplate).Apply(commandBuffer, entity);

        commandBuffer.Set(in entity, new PredefinedOrbit
        {
            Template = new RevolutionOrbit()
            {
                Shape = new(Shape.X, Shape.Y),
                Period = Period,
                Rotation = Rotation
            }
        });
    }
}
