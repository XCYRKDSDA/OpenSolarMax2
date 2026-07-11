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
[Priority((int)GraphicsLayer.Interface)]
[
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(ReferenceSize)),
    ReadCurr(typeof(Projection)),
    ReadCurr(typeof(SelectionRingVisual)),
    ReadCurr(typeof(PlanetSelectionRing.AsRing)),
    ReadCurr(typeof(ViewSelectionRing.AsRing))
]
public sealed partial class VisualizeSelectionRingsSystem(
    World world,
    GraphicsDevice graphicsDevice,
    [Section("systems:visualization:maneuvering_ships_status")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _ringRadiusFactor = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _ringThickness = configs.RequireValue<float>("ring:thickness");
    private readonly Color _selectedRingColor = configs.RequireValue<Color>("ring:selected:color");

    private readonly CircleRenderer _circleRenderer = new(graphicsDevice);

    public void Update() => DrawSelectionRingsQuery(world);

    [Query]
    [All<Projection>]
    private void DrawSelectionRings(Entity entity, in Projection projection)
    {
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        _circleRenderer.Effect.Projection = projection.ScreenToNdc;

        if (!entity.Has<ViewSelectionRing.AsView>())
            return;

        foreach (var (_, record) in entity.Get<ViewSelectionRing.AsView>().Relationships)
        {
            var ring = record.Ring;

            if (!ring.Has<SelectionRingVisual>())
                continue;

            var visual = ring.Get<SelectionRingVisual>();
            if (visual.Alpha <= 0)
                continue;

            if (!ring.Has<PlanetSelectionRing.AsRing>())
                continue;

            var planetRecord = ring.Get<PlanetSelectionRing.AsRing>().Relationship;
            if (planetRecord == null)
                continue;

            var planet = planetRecord.Value.Copy.Planet;
            if (
                !planet.IsAlive()
                || !planet.Has<ReferenceSize>()
                || !planet.Has<AbsoluteTransform>()
            )
                continue;

            var planetCompos = planet.Get<ReferenceSize, AbsoluteTransform>();
            ref readonly var refSize = ref planetCompos.t0;
            ref readonly var pose = ref planetCompos.t1;

            var scale2D = Vector2.TransformNormal(Vector2.One, projection.WorldToScreen);
            var screenScale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));
            var ringRadius = refSize.Radius * _ringRadiusFactor * visual.Scale * screenScale;

            var ringInScreen = TransformProjection.To2D(
                Vector3.Transform(pose.Translation, projection.WorldToScreen)
            );

            var color = _selectedRingColor * visual.Alpha;

            _circleRenderer.DrawCircle(ringInScreen, ringRadius, color, _ringThickness);
        }
    }
}
