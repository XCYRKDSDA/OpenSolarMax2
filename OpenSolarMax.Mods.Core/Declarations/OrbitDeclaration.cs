using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Declarations;

public class OrbitDeclaration
{
    public Vector2? Shape { get; set; }

    public float? Period { get; set; }

    public float? Phase { get; set; }

    public OrbitDeclaration Aggregate(OrbitDeclaration newCfg) => new()
    {
        Shape = newCfg.Shape ?? Shape,
        Period = newCfg.Period ?? Period,
        Phase = newCfg.Phase ?? Phase
    };
}
