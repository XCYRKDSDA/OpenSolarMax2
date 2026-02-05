using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.Transformable), SchemaName("transformable")]
public class TransformableConfiguration : IConfiguration<TransformableDescription, TransformableConfiguration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    public TransformableConfiguration Aggregate(TransformableConfiguration @new)
    {
        return new TransformableConfiguration()
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
