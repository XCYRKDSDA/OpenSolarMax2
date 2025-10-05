using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[InputSystem]
public partial class CleanEventsSystem(World world) : ICoreUpdateWithStructuralChangesSystem
{
    [Query]
    [All<InputEvent>]
    private static void DestroyEvents(Entity entity, [Data] CommandBuffer commandBuffer)
    {
        commandBuffer.Destroy(entity);
    }

    public void Update(GameTime _, CommandBuffer commandBuffer) => DestroyEventsQuery(world, commandBuffer);
}
