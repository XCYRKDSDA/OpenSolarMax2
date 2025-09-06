using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class RelativeTransformTemplate : ITemplate
{
    #region Configurations

    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }

    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    #endregion

    private static readonly Archetype _archetype = new(
        typeof(TreeRelationship<RelativeTransform>),
        typeof(RelativeTransform)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        entity.Set(new TreeRelationship<RelativeTransform>(Parent, Child));
        entity.Set(new RelativeTransform(Translation, Rotation));
    }

    public object Clone() => MemberwiseClone();
}
