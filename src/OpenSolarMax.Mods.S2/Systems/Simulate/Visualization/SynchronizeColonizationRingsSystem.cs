using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 同步天体的殖民进度环实体。
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(Colonizable))]
[ReadCurr(typeof(ColonizationState))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(ColonizationRingDisplayData))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(PlanetColonizationRing.AsPlanet))]
[Calc(typeof(Shape))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Shape))]
[FineWith(typeof(SynchronizeAnchoredShipsArcsAndLabelsSystem), "写入不同的视觉实体", typeof(Shape))]
[DelayedCalc]
public sealed partial class SynchronizeColonizationRingsSystem(
    World world,
    IConceptFactory factory,
    [Section("systems:visualization:colonization")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly float _radiusMultiplier = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _thickness = configs.RequireValue<float>("ring:thickness");
    private readonly float _defaultAlpha = configs.RequireValue<float>("ring:default_alpha");

    private void ConfigureColonizationRing(
        Entity ring,
        ColonizationRingPart part,
        in ReferenceSize size,
        Color color,
        float angle,
        float projectionScale
    )
    {
        var (startAngle, sweepAngle, alpha) = part switch
        {
            ColonizationRingPart.Progress => (MathHelper.Pi * 1.5f - angle / 2, angle, 1),
            ColonizationRingPart.Remainder => (
                MathHelper.Pi * 1.5f + angle / 2,
                MathHelper.TwoPi - angle,
                _defaultAlpha
            ),
            _ => throw new NotImplementedException(),
        };

        ref var shape = ref ring.Get<Shape>();
        shape.Geometry = new Shape.Arc(
            Vector2.Zero,
            size.Radius * _radiusMultiplier * projectionScale,
            _thickness,
            startAngle,
            sweepAngle
        );
        shape.Fill = color;
        shape.Alpha = alpha;
    }

    private void MakeColonizationRings(Entity planet, Entity view, CommandBuffer commandBuffer)
    {
        var progress = factory.Make(
            world,
            commandBuffer,
            ConceptNames.ColonizationArc,
            new ColonizationArcDescription { Planet = planet, View = view }
        );
        var remainder = factory.Make(
            world,
            commandBuffer,
            ConceptNames.ColonizationArc,
            new ColonizationArcDescription { Planet = planet, View = view }
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PlanetColonizationRing,
            new PlanetColonizationRingDescription
            {
                Planet = planet,
                Progress = progress,
                Remainder = remainder,
            }
        );
    }

    [Query]
    [All<Projection>]
    private void SynchronizeColonizationRingForEachView(
        Entity view,
        in Projection projection,
        [Data] CommandBuffer commandBuffer,
        [Data] Entity planet,
        [Data] ReferenceSize size,
        [Data] ColonizationRingDisplayData data,
        [Data] in PlanetColonizationRing.AsPlanet asPlanet
    )
    {
        // 查找现存的实体
        PlanetColonizationRing? existing = null;
        foreach (
            var relationship in asPlanet.Relationships.Values.Where(r =>
                r.Progress.ProjectsTo(view)
            )
        )
        {
            if (existing is null)
            {
                existing = relationship;
                continue;
            }

            commandBuffer.Destroy(relationship.Progress);
            commandBuffer.Destroy(relationship.Remainder);
        }

        if (existing is not { } relation)
        {
            MakeColonizationRings(planet, view, commandBuffer);
            return;
        }

        ConfigureColonizationRing(
            relation.Progress,
            ColonizationRingPart.Progress,
            in size,
            data.Color,
            data.Angle,
            projection.Scale
        );
        ConfigureColonizationRing(
            relation.Remainder,
            ColonizationRingPart.Remainder,
            in size,
            data.Color,
            data.Angle,
            projection.Scale
        );
    }

    [Query]
    [All<
        AnchoredShipsRegistry,
        Colonizable,
        ColonizationState,
        ReferenceSize,
        PlanetColonizationRing.AsPlanet,
        ColonizationRingDisplayData
    >]
    private void SynchronizeColonizationRing(
        Entity planet,
        in AnchoredShipsRegistry ships,
        in Colonizable colonizable,
        in ColonizationState state,
        in ReferenceSize size,
        in PlanetColonizationRing.AsPlanet asPlanet,
        in ColonizationRingDisplayData display,
        [Data] CommandBuffer commandBuffer
    )
    {
        var active =
            ships.Ships.Count == 1
            && state.Team != Entity.Null
            && state.Progress < colonizable.Volume;

        if (!active)
        {
            // 如果根本不应当显示殖民圈，则直接移除所有相关 UI 实体
            foreach (var relationship in asPlanet.Relationships.Values)
            {
                commandBuffer.Destroy(relationship.Progress);
                commandBuffer.Destroy(relationship.Remainder);
            }
            return;
        }

        SynchronizeColonizationRingForEachViewQuery(
            world,
            commandBuffer,
            planet,
            size,
            display,
            in asPlanet
        );
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeColonizationRingQuery(world, commandBuffer);
}
