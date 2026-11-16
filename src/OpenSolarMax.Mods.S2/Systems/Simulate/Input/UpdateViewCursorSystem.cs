using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 把视图的鼠标屏幕坐标反投影为世界坐标，写入该视图光标实体的位姿
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(MouseDeviceState))]
[ReadCurr(typeof(ViewCursor.AsCursor))]
[Calc(typeof(AbsoluteTransform))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(AbsoluteTransform))]
[FineWith(
    typeof(CalculateAbsoluteTransformSystem),
    "光标不在变换树中，位姿由输入反投影得出，与绝对变换计算无数据依赖",
    typeof(AbsoluteTransform)
)]
[FineWith(
    typeof(CalculateShipPositionSystem),
    "光标只由视图投影与鼠标决定，与舰船位置计算互不影响",
    typeof(AbsoluteTransform)
)]
[FineWith(
    typeof(ApplyShipsWarpingEffectSystem),
    "光标只由视图投影与鼠标决定，与跃迁效果计算互不影响",
    typeof(AbsoluteTransform)
)]
public sealed partial class UpdateViewCursorSystem(World world) : ICalcSystem
{
    [Query]
    [All<ViewCursor.AsCursor>]
    private static void UpdateCursor(Entity entity, in ViewCursor.AsCursor asCursor)
    {
        var view = asCursor.Relationship!.Value.Copy.View;
        ref readonly var projection = ref view.Get<Projection>();
        ref readonly var mouse = ref view.Get<MouseDeviceState>();

        var worldPosition = Vector3.Transform(
            new Vector3(mouse.State.Position.ToVector2(), 0),
            Matrix.Invert(projection.WorldToScreen)
        );
        entity.Get<AbsoluteTransform>().Translation = worldPosition;
    }

    public void Update() => UpdateCursorQuery(world);
}
