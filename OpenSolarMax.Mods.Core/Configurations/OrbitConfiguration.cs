using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Configurations;

public class OrbitConfiguration
{
    public Vector2? Shape { get; set; }

    public float? Period { get; set; }

    public float? Phase { get; set; }

    public OrbitConfiguration Aggregate(OrbitConfiguration newCfg) => new()
    {
        Shape = newCfg.Shape ?? Shape,
        Period = newCfg.Period ?? Period,
        Phase = newCfg.Phase ?? Phase
    };
}
