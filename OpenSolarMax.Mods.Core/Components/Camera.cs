using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 相机组件。描述成像参数
/// </summary>
[Component]
public struct Camera
{
    public float Width;
    public float Height;

    public float ZNear;
    public float ZFar;
}
