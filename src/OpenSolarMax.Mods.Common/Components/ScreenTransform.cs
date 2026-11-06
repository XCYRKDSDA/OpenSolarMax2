using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Common.Components;

[Component]
public struct ScreenTransform()
{
    public Vector2 Position;

    public float Rotation;
}
