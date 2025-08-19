using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("turret")]
public class TurretConfiguration : IEntityConfiguration, ITransformableConfiguration
{
    #region Transformable

    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    #endregion

    public string? Party { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not PortalConfiguration newCfg) throw new InvalidDataException();

        return new PortalConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Party = newCfg.Party ?? Party
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        var template = new TurretTemplate(assets);

        template.Transform = (this as ITransformableConfiguration).ParseOptions(ctx);

        if (Party is not null)
            template.Party = ctx.OtherEntities[Party];

        return template;
    }
}
