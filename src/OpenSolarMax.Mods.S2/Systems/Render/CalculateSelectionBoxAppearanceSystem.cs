using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 更新每个视图选框实体的位置、尺寸与颜色
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(InputGesture))]
[ReadCurr(typeof(ViewSelectionBox.AsBox))]
[Calc(typeof(Shape))]
[Calc(typeof(ScreenTransform))]
[FineWith(typeof(ProjectWorldToScreenSystem), "选框位置不从世界投影而来", typeof(ScreenTransform))]
public sealed partial class CalculateSelectionBoxAppearanceSystem(
    World world,
    [Section("systems:visualization:selection_box")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _thickness = configs.RequireValue<float>("thickness");
    private readonly Color _color = configs.RequireValue<Color>("color");

    [Query]
    [All<ViewSelectionBox.AsBox, Shape, ScreenTransform>]
    private void UpdateBoxGeometry(
        in ViewSelectionBox.AsBox asBox,
        ref Shape shape,
        ref ScreenTransform transform
    )
    {
        var view = asBox.Relationship!.Value.Copy.View;
        var dragging = view.Get<InputGesture>().State.AsT2;

        var rectangle = new Rectangle(
            new Point(
                Math.Min(dragging.CurrentPosition.X, dragging.PressedPosition.X),
                Math.Min(dragging.CurrentPosition.Y, dragging.PressedPosition.Y)
            ),
            new Point(
                Math.Abs(dragging.CurrentPosition.X - dragging.PressedPosition.X),
                Math.Abs(dragging.CurrentPosition.Y - dragging.PressedPosition.Y)
            )
        );

        shape.Geometry = new Shape.Rectangle(
            Vector2.Zero,
            new Vector2(rectangle.Width, rectangle.Height),
            _thickness,
            0,
            Shape.ThicknessAlignment.Centered
        );
        shape.Fill = _color;
        shape.Alpha = 1f;

        transform.Position = new Vector2(rectangle.X, rectangle.Y);
        transform.Rotation = 0;
    }

    public void Update() => UpdateBoxGeometryQuery(world);
}
