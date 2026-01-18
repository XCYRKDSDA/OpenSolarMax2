using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 可生产组件。
/// 拥有该组件的阵营可以进行生产。
/// 字段描述生产一个该阵营单位需要的工作量
/// </summary>
[Component]
public struct Producible
{
    /// <summary>
    /// 生产该类型单位需要的工作量
    /// </summary>
    public float WorkloadPerShip;
}
