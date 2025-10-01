using Arch.Buffer;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 执行结构化变更的系统，并且使用外置的<see cref="CommandBuffer"/>
/// </summary>
public interface IStructuralChangeSystem
{
    void Initialize(CommandBuffer commandBuffer);

    void Update(GameTime gameTime, CommandBuffer commandBuffer);
}
