using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework.Input;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 根据鼠标左键的按下与松开，为每个视图创建和销毁光标实体
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(MouseDeviceState))]
[ReadCurr(typeof(ViewCursor.AsView))]
[DelayedCalc]
public sealed partial class SynchronizeViewCursorSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<ViewCursor.AsView, MouseDeviceState>]
    private void Synchronize(
        Entity view,
        in ViewCursor.AsView asView,
        in MouseDeviceState mouse,
        [Data] CommandBuffer commandBuffer
    )
    {
        var pressed = mouse.State.LeftButton == ButtonState.Pressed;
        if (pressed == asView.Relationship.HasValue)
            return;

        if (pressed)
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.Cursor,
                new CursorDescription { View = view }
            );
        else
            commandBuffer.Destroy(asView.Relationship!.Value.Copy.Cursor);
    }

    public void Update(CommandBuffer commandBuffer) => SynchronizeQuery(world, commandBuffer);
}
