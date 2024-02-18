using Arch.System;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.System;

public interface IUpdateSystem : ISystem<GameTime>
{ }

public interface IDrawSystem : ISystem<GameTime>
{ }
