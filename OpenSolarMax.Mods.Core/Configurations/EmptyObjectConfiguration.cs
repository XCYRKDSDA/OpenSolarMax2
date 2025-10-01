using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("empty")]
public class EmptyObjectConfiguration : IEntityConfiguration, ITransformableConfiguration
{
    #region Transformable

    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    #endregion

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not EmptyObjectConfiguration newCfg) throw new InvalidDataException();

        return new EmptyObjectConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        var template = new EmptyCoordTemplate();

        template.Transform = (this as ITransformableConfiguration).ParseOptions(ctx);

        return template;
    }
}
