using Microsoft.Xna.Framework;
using OpenSolarMax.Ecs;

namespace OpenSolarMax.Mods.Common.Components;

[Component]
public struct ScreenTransform()
{
    public Vector2 Position;

    public float Rotation;

    public float Z;
}
