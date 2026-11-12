using Apos.Shapes;
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
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 按视图的点选状态同步悬停发光实体。
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(PointingStatus))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(Sprite))]
[ReadCurr(typeof(PointedEntityHoverGlow.AsView))]
[Calc(typeof(Shape))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Shape))]
[FineWith(typeof(SynchronizeColonizationRingsSystem), "写入不同的视觉实体", typeof(Shape))]
[FineWith(typeof(SynchronizeAnchoredShipsArcsAndLabelsSystem), "写入不同的视觉实体", typeof(Shape))]
[DelayedCalc]
public sealed partial class SynchronizeHoverGlowSystem(
    World world,
    IConceptFactory factory,
    [Section("systems:visualization:hover_glow")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly float _radiusMultiplier = configs.RequireValue<float>("radius_multiplier");
    private readonly float _offset = configs.RequireValue<float>("offset");
    private readonly float _innerFactor = configs.RequireValue<float>("inner_factor");
    private readonly float _alpha = configs.RequireValue<float>("alpha");

    private void ConfigureHoverGlow(Entity glow, Entity pointedEntity, in Projection projection)
    {
        ref readonly var size = ref pointedEntity.Get<ReferenceSize>();
        ref readonly var sprite = ref pointedEntity.Get<Sprite>();

        var outerRadius = size.Radius * _radiusMultiplier * projection.Scale + _offset;
        var innerRadius = size.Radius * _innerFactor * projection.Scale;

        ref var shape = ref glow.Get<Shape>();
        shape.Geometry = new Shape.Circle(Vector2.Zero, outerRadius);
        shape.Fill = new Gradient(
            Vector2.Zero,
            Color.Transparent,
            new Vector2(outerRadius, 0),
            sprite.Color,
            Gradient.Shape.Radial,
            Gradient.RepeatStyle.None,
            innerRadius,
            0,
            false
        );
        shape.Alpha = _alpha;
    }

    [Query]
    [All<PointingStatus, Projection, PointedEntityHoverGlow.AsView>]
    private void SynchronizeHoverGlow(
        Entity view,
        in PointingStatus pointing,
        in Projection projection,
        in PointedEntityHoverGlow.AsView asView,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (pointing.PointedEntity is not { } pointedEntity)
        {
            // 如果指向的天体为空，则清除所有的内发光对象
            foreach (var relationship in asView.Relationships.Values)
                commandBuffer.Destroy(relationship.Glow);

            return;
        }

        // 遍历查找现有的内发光实体，顺便清理多余的实体
        Entity? existingGlow = null;
        foreach (var relationship in asView.Relationships.Values)
        {
            if (relationship.PointedEntity == pointedEntity && existingGlow is null)
            {
                existingGlow = relationship.Glow;
                continue;
            }

            commandBuffer.Destroy(relationship.Glow);
        }

        if (existingGlow is not { } theGlow)
        {
            // 如果没有现存的内发光实体则新建一个，但是等到不动点迭代循环时再来配置细节
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.HoverGlow,
                new HoverGlowDescription { PointedEntity = pointedEntity, View = view }
            );
            return;
        }

        ConfigureHoverGlow(theGlow, pointedEntity, in projection);
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeHoverGlowQuery(world, commandBuffer);
}
