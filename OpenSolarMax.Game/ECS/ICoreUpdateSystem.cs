using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.ECS;

public interface ICoreUpdateSystem
{
    void Update(GameTime gameTime);
}
