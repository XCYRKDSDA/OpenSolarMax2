using Arch.Buffer;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.ECS;

public interface ICoreUpdateWithStructuralChangesSystem
{
    void Update(GameTime gameTime, CommandBuffer commandBuffer);
}
