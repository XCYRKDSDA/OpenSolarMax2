using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.Transformable), SchemaName("transformable")]
public class TransformableDeclaration : IDeclaration<TransformableDescription, TransformableDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public TransformableDeclaration Aggregate(TransformableDeclaration @new)
    {
        return new TransformableDeclaration()
        {
            Parent = @new.Parent ?? Parent,
            Position = @new.Position ?? Position,
            Orbit = Orbit is not null && @new.Orbit is not null
                        ? Orbit.Aggregate(@new.Orbit)
                        : @new.Orbit ?? Orbit,
        };
    }

    public TransformableDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Parent is null)
        {
            var transform = new AbsoluteTransformOptions();
            if (Position is { } position)
                transform.Translation = new Vector3(position.X, position.Y, 0);
            return new TransformableDescription() { Transform = transform };
        }
        else if (Orbit is null)
        {
            var transform = new RelativeTransformOptions() { Parent = otherEntities[Parent] };
            if (Position is { } position)
                transform.Translation = new Vector3(position.X, position.Y, 0);
            return new TransformableDescription() { Transform = transform };
        }
        else
        {
            if (Parent is null || Orbit.Shape is null || Orbit.Period is null) throw new NullReferenceException();

            var revolution = new RevolutionOptions()
            {
                Parent = otherEntities[Parent],
                Shape = new(Orbit.Shape.Value.X, Orbit.Shape.Value.Y),
                Period = Orbit.Period.Value
            };

            if (Orbit.Phase is not null)
                revolution.InitPhase = Orbit.Phase.Value * MathF.PI * 2;

            return new TransformableDescription() { Transform = revolution };
        }
    }
}
