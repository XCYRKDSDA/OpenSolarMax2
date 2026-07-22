using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[Disable]
[LateUpdate]
[SimulateSystem]
[Consume(typeof(InputEvent))]
[ChangeStructure]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public partial class CleanEventsSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<InputEvent>]
    private static void DestroyEvents(Entity entity, [Data] CommandBuffer commandBuffer)
    {
        commandBuffer.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => DestroyEventsQuery(world, commandBuffer);
}
