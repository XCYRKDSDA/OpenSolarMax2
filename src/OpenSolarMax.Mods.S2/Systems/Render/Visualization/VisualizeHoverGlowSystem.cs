using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.Common.Utils;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Graphics;

namespace OpenSolarMax.Mods.S2.Systems;

[LateUpdate]
[RenderSystem]
[Priority((int)GraphicsLayer.Interface - 1)] // 内发光位于选择圈底部，要先行绘制
[ReadCurr(typeof(PointingStatus))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(Sprite))]
public sealed partial class VisualizeHoverGlowSystem(
    World world,
    GraphicsDevice graphicsDevice,
    [Section("systems:visualization:hover_glow")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _radiusMultiplier = configs.RequireValue<float>("radius_multiplier");
    private readonly float _offset = configs.RequireValue<float>("offset");
    private readonly float _innerFactor = configs.RequireValue<float>("inner_factor");
    private readonly float _alpha = configs.RequireValue<float>("alpha");

    private readonly GlowCircleRenderer _glowRenderer = new(graphicsDevice);

    public void Update() => DrawHoverGlowQuery(world);

    [Query]
    [All<PointingStatus, Projection>]
    private void DrawHoverGlow(in PointingStatus pointing, in Projection projection)
    {
        if (pointing.PointedEntity is not { } hoveringEntity)
            return;

        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _glowRenderer.Effect.Projection = projection.ScreenToNdc;

        var compos = hoveringEntity.Get<ReferenceSize, AbsoluteTransform, Sprite>();
        ref readonly var refSize = ref compos.t0;
        ref readonly var pose = ref compos.t1;
        ref readonly var sprite = ref compos.t2;

        // 发光带外缘必须与细环同一半径公式（含 offset），保证完全重合
        var scale2D = Vector2.TransformNormal(Vector2.One, projection.WorldToScreen);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));
        var outerRadius = refSize.Radius * _radiusMultiplier * scale + _offset;
        var innerRadius = refSize.Radius * _innerFactor * scale;

        var ringInScreen = TransformProjection.To2D(
            Vector3.Transform(pose.Translation, projection.WorldToScreen)
        );

        var glowColor = sprite.Color * _alpha;
        _glowRenderer.DrawGlowCircle(ringInScreen, outerRadius, innerRadius, glowColor);
    }
}
