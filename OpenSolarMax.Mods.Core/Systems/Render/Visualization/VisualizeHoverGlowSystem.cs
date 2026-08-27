using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdate]
[RenderSystem]
[Priority((int)GraphicsLayer.Interface - 1)] // 内发光位于选择圈底部，要先行绘制
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
    private readonly int _minimalHitPixels = configs.RequireValue<int>("minimal_hit_pixels");
    private readonly float _radiusMultiplier = configs.RequireValue<float>("radius_multiplier");
    private readonly float _offset = configs.RequireValue<float>("offset");
    private readonly float _innerFactor = configs.RequireValue<float>("inner_factor");
    private readonly float _alpha = configs.RequireValue<float>("alpha");

    private readonly GlowCircleRenderer _glowRenderer = new(graphicsDevice);

    public void Update() => DrawHoverGlowQuery(world);

    [Query]
    [All<Projection>]
    private void DrawHoverGlow(in Projection projection)
    {
        var mouse = Mouse.GetState();
        var mouseInScreen = mouse.Position;

        Entity? pointedPlanet = null;
        var hoveringPlanet = pointedPlanet ??= GetHoveredPlanet(
            in mouseInScreen,
            in projection.WorldToScreen
        );

        if (hoveringPlanet == Entity.Null)
            return;

        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _glowRenderer.Effect.Projection = projection.ScreenToNdc;

        var compos = hoveringPlanet.Get<ReferenceSize, AbsoluteTransform, Sprite>();
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

    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AbsoluteTransform>]
    private void CheckHoveredPlanet(
        Entity planet,
        in AbsoluteTransform pose,
        [Data] in Point mouseInScreen,
        [Data] in Matrix worldToScreen,
        [Data] ref Entity pointedPlanet,
        [Data] ref float pointedPlanetZ
    )
    {
        float radiusInScreen = _minimalHitPixels;
        if (planet.Has<ReferenceSize>()) // 若对象没有参考尺寸，则按照最小命中像素数判定范围；否则按照参考尺寸判定，但不得小于最小值
        {
            ref readonly var refSize = ref planet.Get<ReferenceSize>();
            var halfSizeInScreen = Vector2.TransformNormal(new(refSize.Radius), worldToScreen);
            radiusInScreen = MathF.Max(
                MathF.Abs(MathF.MaxMagnitude(halfSizeInScreen.X, halfSizeInScreen.Y)),
                radiusInScreen
            );
        }

        var positionInScreen = Vector3.Transform(pose.Translation, worldToScreen);
        var delta = new Vector2(
            positionInScreen.X - mouseInScreen.X,
            positionInScreen.Y - mouseInScreen.Y
        );
        if (delta.Length() < radiusInScreen && positionInScreen.Z < pointedPlanetZ)
        {
            pointedPlanet = planet;
            pointedPlanetZ = positionInScreen.Z;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entity GetHoveredPlanet(in Point mouseInScreen, in Matrix worldToScreen)
    {
        Entity pointedPlanet = Entity.Null;
        float pointedPlanetZ = float.PositiveInfinity;
        CheckHoveredPlanetQuery(
            world,
            in mouseInScreen,
            in worldToScreen,
            ref pointedPlanet,
            ref pointedPlanetZ
        );
        return pointedPlanet;
    }
}
