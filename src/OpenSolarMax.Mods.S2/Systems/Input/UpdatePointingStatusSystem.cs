using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 点选判定系统。为每个视图计算鼠标下方屏幕深度最前的可点选实体，写入 <see cref="PointingStatus"/>。
/// 命中半径取 Pickable 的屏幕投影，但不小于最小选择像素数。
/// </summary>
[LateUpdate]
[InputSystem]
[
    ReadCurr(typeof(MouseDeviceState)),
    ReadCurr(typeof(Projection)),
    ReadCurr(typeof(AbsoluteTransform)),
    Calc(typeof(PointingStatus))
]
public sealed partial class UpdatePointingStatusSystem(
    World world,
    [Section("systems:input")] IConfiguration configs
) : ICalcSystem
{
    private readonly int _minimalSelectPixels = configs.RequireValue<int>("minimal_select_pixels");

    public void Update() => UpdatePointingQuery(world);

    [Query]
    [All<MouseDeviceState, Projection, PointingStatus>]
    private void UpdatePointing(
        in MouseDeviceState mouse,
        in Projection projection,
        ref PointingStatus pointing
    )
    {
        var pointedEntity = Entity.Null; // Arch Query 方法没法传 Nullable，只能回退使用 Entity.Null 表达空
        var pointedEntityZ = float.PositiveInfinity;
        var mouseInScreen = mouse.State.Position;
        CheckPointedQuery(
            world,
            in mouseInScreen,
            in projection.WorldToScreen,
            ref pointedEntity,
            ref pointedEntityZ
        );
        pointing.PointedEntity = pointedEntity == Entity.Null ? null : pointedEntity;
    }

    [Query]
    [All<Pickable, AbsoluteTransform>]
    private void CheckPointed(
        Entity entity,
        in Pickable pickable,
        in AbsoluteTransform pose,
        [Data] in Point mouseInScreen,
        [Data] in Matrix worldToScreen,
        [Data] ref Entity pointedEntity, // 可空类型不能作 Data 参数：生成器以参数类型拼文件名，问号为非法字符
        [Data] ref float pointedEntityZ
    )
    {
        var halfSizeInScreen = Vector2.TransformNormal(new(pickable.HitRadius), worldToScreen);
        var hitRadiusInScreen = MathF.Max(
            MathF.Abs(MathF.MaxMagnitude(halfSizeInScreen.X, halfSizeInScreen.Y)),
            _minimalSelectPixels
        );

        var positionInScreen = Vector3.Transform(pose.Translation, worldToScreen);
        var delta = new Vector2(
            positionInScreen.X - mouseInScreen.X,
            positionInScreen.Y - mouseInScreen.Y
        );

        if (delta.Length() < hitRadiusInScreen && positionInScreen.Z < pointedEntityZ)
        {
            pointedEntity = entity;
            pointedEntityZ = positionInScreen.Z;
        }
    }
}
