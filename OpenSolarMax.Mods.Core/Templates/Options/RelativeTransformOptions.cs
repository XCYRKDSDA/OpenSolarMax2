using Arch.Core;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Templates.Options;

public class RelativeTransformOptions
{
    public required Entity Parent { get; set; }

    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}
