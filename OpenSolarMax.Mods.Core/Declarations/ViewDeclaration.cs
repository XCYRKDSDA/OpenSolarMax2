using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.View), SchemaName("view")]
public class ViewDeclaration : IDeclaration<ViewDescription, ViewDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public int[]? Size { get; set; }

    public float[]? Depth { get; set; }

    public string? Party { get; set; }

    public ViewDeclaration Aggregate(ViewDeclaration newCfg)
    {
        return new ViewDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Size = newCfg.Size ?? Size,
            Depth = newCfg.Depth ?? Depth,
            Party = newCfg.Party ?? Party
        };
    }

    public ViewDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Party is null) throw new NullReferenceException();

        var desc = new ViewDescription()
        {
            Party = otherEntities[Party],
        };

        var tfCfg = new TransformableDeclaration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        if (Size is not null)
            desc.Size = new Point(Size[0], Size[1]);
        if (Depth is not null)
            desc.Depth = (Depth[0], Depth[1]);

        return desc;
    }
}
