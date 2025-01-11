using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class EmptyCoordTemplate : ITemplate, ITransformableTemplate
{
    #region Options

    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions>
        Transform { get; set; } = new AbsoluteTransformOptions();

    #endregion

    private static readonly Archetype _archetype = new(
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        (this as ITransformableTemplate).Apply(entity);
    }
}
