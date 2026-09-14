using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Utils;

public struct Angle(float radians = 0)
{
    [ConfigurationKeyName("rad")]
    public float Radians { get; set; } = radians;

    [ConfigurationKeyName("deg")]
    public float Degrees
    {
        get => MathHelper.ToDegrees(Radians);
        set => Radians = MathHelper.ToRadians(value);
    }

    public static implicit operator float(in Angle angle) => angle.Radians;

    public static implicit operator Angle(float radians) => new(radians);
}
