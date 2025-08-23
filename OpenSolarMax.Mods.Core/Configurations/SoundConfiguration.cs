using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("sound")]
public class SoundConfiguration : IEntityConfiguration, ITransformableConfiguration
{
    #region Transformable

    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    #endregion

    public string? Sound { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not SoundConfiguration newCfg) throw new InvalidDataException();

        return new SoundConfiguration()
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
        if (string.IsNullOrEmpty(Sound)) throw new NullReferenceException();

        var template = new SimpleSoundTemplate() { SoundEffect = assets.Load<FmodEventDescription>(Sound) };

        template.Transform = (this as ITransformableConfiguration).ParseOptions(ctx);

        return template;
    }
}
