using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 按世界实体到屏幕实体的投影关系，把世界位姿写到屏幕变换上。
/// </summary>
[RenderSystem, LateUpdate]
[ReadCurr(typeof(WorldToScreenProjection.AsTarget))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(AbsoluteTransform))]
[Calc(typeof(ScreenTransform))]
public sealed partial class ProjectWorldToScreenSystem(World world) : ICalcSystem
{
    [Query]
    [All<WorldToScreenProjection.AsTarget, ScreenTransform>]
    private static void Project(
        in WorldToScreenProjection.AsTarget asTarget,
        ref ScreenTransform target
    )
    {
        if (asTarget.Relationship is not { } relationship)
            return;

        var relation = relationship.Copy;
        ref readonly var source = ref relation.Source.Get<AbsoluteTransform>();
        ref readonly var projection = ref relation.View.Get<Projection>();

        var screen = Vector3.Transform(source.Translation, projection.WorldToScreen);
        target.Position = new Vector2(screen.X, screen.Y);

        if (!relation.ProjectRotation)
            return;

        var worldAxis = Vector3.Transform(Vector3.UnitX, source.Rotation);
        var screenAxis = Vector3.TransformNormal(worldAxis, projection.WorldToScreen);
        target.Rotation = MathF.Atan2(screenAxis.Y, screenAxis.X);
    }

    public void Update() => ProjectQuery(world);
}
