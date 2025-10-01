using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("view")]
public class ViewConfiguration : IEntityConfiguration, ITransformableConfiguration
{
    #region Transformable

    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    #endregion

    public int[]? Size { get; set; }

    public float[]? Depth { get; set; }

    public string? Party { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not ViewConfiguration newCfg) throw new InvalidDataException();

        return new ViewConfiguration()
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

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        if (Party is null) throw new NullReferenceException();
        var template = new ViewTemplate()
        {
            Party = ctx.OtherEntities[Party]
        };

        template.Transform = (this as ITransformableConfiguration).ParseOptions(ctx);

        if (Size is not null)
            template.Size = new(Size[0], Size[1]);
        if (Depth is not null)
            template.Depth = (Depth[0], Depth[1]);

        return template;
    }
}
