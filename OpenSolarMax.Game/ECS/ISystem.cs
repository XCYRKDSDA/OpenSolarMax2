using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 限制使用<see cref="GameTime"/>作为系统更新参数
/// </summary>
public interface ISystem : ISystem<GameTime>
{
    /// <summary>
    /// 修改其他系统的入口方法<br/>
    /// 允许模组系统修改其他系统的配置。该方法将在所有系统构造完毕后执行
    /// </summary>
    /// <param name="systems"></param>
    void ModifyOthers(ISystemProvider systems) { }
}
