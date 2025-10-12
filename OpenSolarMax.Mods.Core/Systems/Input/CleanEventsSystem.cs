using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[InputSystem, BeforeStructuralChanges, ChangeStructure]
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
