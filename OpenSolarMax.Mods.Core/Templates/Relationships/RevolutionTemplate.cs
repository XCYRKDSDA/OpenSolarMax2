using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

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

    private static readonly Archetype _archetype = new(
        typeof(TreeRelationship<RelativeTransform>),
        typeof(RelativeTransform),
        typeof(RevolutionOrbit),
        typeof(RevolutionState)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        entity.Set(new TreeRelationship<RelativeTransform>(Parent, Child));

        ref var orbit = ref entity.Get<RevolutionOrbit>();
        orbit.Shape = Shape;
        orbit.Period = Period;
        orbit.Rotation = Rotation;

        ref var state = ref entity.Get<RevolutionState>();
        state.Phase = InitPhase;
    }
}
