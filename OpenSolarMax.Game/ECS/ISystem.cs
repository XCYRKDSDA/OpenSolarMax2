using Arch.System;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 限制使用<see cref="GameTime"/>作为系统更新参数
/// </summary>
public interface ISystem : ISystem<GameTime>
{ }
