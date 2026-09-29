using Nine.Assets;
using Nine.Screens;

namespace OpenSolarMax.Game;

/// <summary>
/// 界面层依赖注入点
/// 包含全部后端服务（<see cref="IGameServices"/>），并补充界面专用依赖
/// </summary>
public interface IUiServices : IGameServices
{
    /// <summary>全局资产管理器</summary>
    AssetsManager Assets { get; }

    /// <summary>全局界面管理器</summary>
    ScreenManager ScreenManager { get; }

    /// <summary>串行式后台加载调度器</summary>
    TaskScheduler BackgroundScheduler { get; }
}
