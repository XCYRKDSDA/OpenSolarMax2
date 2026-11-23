using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 点选状态。记录当前视图下鼠标位置下最靠前的可点选实体
/// </summary>
[Component]
public struct PointingStatus()
{
    /// <summary>
    /// 当前被指向的可点选实体；没有指向任何实体时为空
    /// </summary>
    public Entity? PointedEntity = null;
}
