using Microsoft.Xna.Framework.Graphics;
using FmodStudioSystem = FMOD.Studio.System;

namespace OpenSolarMax.Game;

/// <summary>
/// 后端依赖注入点
/// </summary>
public interface IGameServices
{
    GraphicsDevice GraphicsDevice { get; }

    FmodStudioSystem FmodSystem { get; }

    IFolders Folders { get; }
}
