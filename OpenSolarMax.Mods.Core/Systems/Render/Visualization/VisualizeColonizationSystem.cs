using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(Projection))]
[Priority((int)GraphicsLayer.Interface)]
public sealed partial class VisualizeColonizationSystem(
    World world,
    GraphicsDevice graphicsDevice,
    [Section("systems:visualization:colonization")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _ringRadiusFactor = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _ringThickness = configs.RequireValue<float>("ring:thickness");
    private readonly float _defaultAlpha = configs.RequireValue<float>("ring:default_alpha");

    private static readonly QueryDescription _planetDesc = new QueryDescription().WithAll<
        AnchoredShipsRegistry,
        Colonizable,
        ColonizationState,
        ReferenceSize,
        AbsoluteTransform
    >();

    private readonly RingRenderer _ringRenderer = new(graphicsDevice);

    public void Update()
    {
        var planetEntities = new List<Entity>();
        world.Query(in _planetDesc, entity => planetEntities.Add(entity));
        RenderToCameraQuery(world, planetEntities);
    }

    private void VisualizeOnePlanet(
        in AnchoredShipsRegistry shipsRegistry,
        in Colonizable colonizable,
        in ColonizationState colonizationState,
        in ReferenceSize refSize,
        in AbsoluteTransform pose,
        in Matrix worldToScreen
    )
    {
        // 当且仅当有一个阵营时绘制占领环
        if (shipsRegistry.Ships.Count != 1)
            return;

        // 当占领完成时不再绘制占领环
        if (colonizationState.Progress >= colonizable.Volume)
            return;

        // 当无人占领时不绘制占领环
        if (colonizationState.Team == Entity.Null)
            return;

        // 计算从世界到屏幕的缩放
        var scale2D = Vector2.TransformNormal(new Vector2(1, 1), worldToScreen);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        // 计算殖民环的尺寸
        var ringRadius = refSize.Radius * _ringRadiusFactor * scale;

        // 获得殖民环的圆心
        var ringCenter = TransformProjection.To2D(
            Vector3.Transform(pose.Translation, worldToScreen)
        );

        // 计算首尾角度
        var angle = MathF.PI * 2 * colonizationState.Progress / colonizable.Volume;
        var head = MathF.PI * 1.5f - angle / 2;

        // 获取颜色
        var color = colonizationState.Team.Get<TeamReferenceColor>().Value;

        _ringRenderer.DrawArc(ringCenter, ringRadius, head, angle, color, _ringThickness);
        _ringRenderer.DrawArc(
            ringCenter,
            ringRadius,
            head + angle,
            MathF.PI * 2 - angle,
            color * _defaultAlpha,
            _ringThickness
        );
    }

    [Query]
    [All<Projection>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Projection projection)
    {
        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _ringRenderer.Effect.Projection = projection.ScreenToNdc;

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity.Get<
                AnchoredShipsRegistry,
                Colonizable,
                ColonizationState,
                ReferenceSize,
                AbsoluteTransform
            >();
            VisualizeOnePlanet(
                in refs.t0,
                in refs.t1,
                in refs.t2,
                in refs.t3,
                in refs.t4,
                in projection.WorldToScreen
            );
        }
    }
}
