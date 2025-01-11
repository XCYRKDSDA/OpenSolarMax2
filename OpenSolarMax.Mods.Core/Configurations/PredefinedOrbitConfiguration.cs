using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("orbit")]
public class PredefinedOrbitConfiguration : IEntityConfiguration, ITransformableConfiguration
{
    #region Transformable

    public string? Parent { get; set; }
    public Vector2? Position { get; set; }
    public OrbitConfiguration? Orbit { get; set; }

    #endregion

    public Vector2? Shape { get; set; }

    public float? Roll { get; set; }

    public float? Period { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not PredefinedOrbitConfiguration newCfg)
            throw new InvalidDataException();

        return new PredefinedOrbitConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Shape = newCfg.Shape ?? Shape,
            Roll = newCfg.Roll ?? Roll,
            Period = newCfg.Period ?? Period
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        if (Shape is null) throw new NullReferenceException();
        if (Period is null) throw new NullReferenceException();

        var template = new PredefinedOrbitTemplate()
        {
            Shape = Shape.Value,
            Period = Period.Value
        };

        template.Transform = (this as ITransformableConfiguration).ParseOptions(ctx);

        if (Roll is not null)
            template.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Roll.Value);

        return template;
    }
}
