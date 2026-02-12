using Arch.Buffer;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Modding.ECS;

public interface ITickSystem
{
    void Update(GameTime gameTime);
}

public interface ITickSystemWithStructuralChanges
{
    void Update(GameTime gameTime, CommandBuffer commandBuffer);
}

public interface ICalcSystem
{
    void Update();
}

public interface ICalcSystemWithStructuralChanges
{
    void Update(CommandBuffer commandBuffer);
}
