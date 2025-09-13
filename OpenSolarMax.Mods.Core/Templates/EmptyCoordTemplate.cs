using Arch.Buffer;
using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Templates;

public class EmptyCoordTemplate : ITemplate, ITransformableTemplate
{
    #region Options

    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions>
        Transform { get; set; } = new AbsoluteTransformOptions();

    #endregion

    private static readonly Signature _signature = new(
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );

    public Signature Signature => _signature;

    public void Apply(Entity entity)
    {
        (this as ITransformableTemplate).Apply(entity);
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        (this as ITransformableTemplate).Apply(commandBuffer, entity);
    }
}
