using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Templates;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Configurations;

public interface ITransformableConfiguration
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }
}

public static class TransformableConfiguration
{
    public static OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions>
        ParseOptions(this ITransformableConfiguration configuration, WorldLoadingContext ctx)
    {
        if (configuration.Parent is null)
        {
            var transform = new AbsoluteTransformOptions();
            if (configuration.Position is { } position)
                transform.Translation = new(position.X, position.Y, 0);
            return transform;
        }
        else if (configuration.Orbit is null)
        {
            var transform = new RelativeTransformOptions() { Parent = ctx.OtherEntities[configuration.Parent] };
            if (configuration.Position is { } position)
                transform.Translation = new(position.X, position.Y, 0);
            return transform;
        }
        else
        {
            if (configuration.Parent is null) throw new NullReferenceException();
            if (configuration.Orbit.Shape is null) throw new NullReferenceException();
            if (configuration.Orbit.Period is null) throw new NullReferenceException();

            var revolution = new RevolutionOptions()
            {
                Parent = ctx.OtherEntities[configuration.Parent],
                Shape = new(configuration.Orbit.Shape.Value.X, configuration.Orbit.Shape.Value.Y),
                Period = configuration.Orbit.Period.Value
            };

            if (configuration.Orbit.Phase is not null)
                revolution.InitPhase = configuration.Orbit.Phase.Value * MathF.PI * 2;

            return revolution;
        }
    }
}
