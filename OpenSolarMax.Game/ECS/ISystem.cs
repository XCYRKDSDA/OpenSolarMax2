using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 执行非结构化变更的系统
/// </summary>
public interface ISystem
{
    void Update(GameTime gameTime);
}
