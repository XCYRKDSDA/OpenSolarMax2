using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Templates.Options;

public class AbsoluteTransformOptions
{
    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}
