using System.Runtime.InteropServices;
using System.Windows.Input;
using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelPlayViewModel : ViewModelBase
{
    private readonly LevelSession _session;
    private readonly Entity _viewEntity;
    private readonly int _levelIndex;

    /// <summary>
    /// 自动退出是否已触发，保证只退出一次
    /// </summary>
    private bool _autoExitHandled;

    [ObservableProperty]
    private bool _paused = false;

    [ObservableProperty]
    private float _simulateSpeed = 1;

    [ObservableProperty]
    private Texture2D _background;

    [ObservableProperty]
    private ICommand _exitCommand;

    public World World => _session.World;

    public AggregateSystem RenderSystem => _session.RenderSystems;

    public Entity ViewEntity => _viewEntity;

    public LevelPlayViewModel(
        LevelSession session,
        Texture2D background,
        int levelIndex,
        SolarMax game
    )
        : base(game)
    {
        // 记录会话
        _session = session;
        _background = background;
        _levelIndex = levelIndex;

        _exitCommand = new RelayCommand(OnExit);

        // 查找相机
        var viewDesc = new QueryDescription().WithAll<ViewTag>();
        var viewCount = _session.World.CountEntities(in viewDesc);
        if (viewCount > 1)
            throw new Exception("there're more than one view entities in the world!");
        if (viewCount <= 0)
            throw new Exception("there's no view entity in the world!");
        _session.World.GetEntities(in viewDesc, MemoryMarshal.CreateSpan(ref _viewEntity, 1));
    }

    partial void OnPausedChanged(bool value) => _session.Paused = value;

    partial void OnSimulateSpeedChanged(float value) => _session.SimulateSpeed = value;

    private void OnExit()
    {
        // 避免在正在过渡时触发过渡
        if (Game.ScreenManager.Transitioning)
            return;
        Game.ScreenManager.Backward();
    }

    public override void OnActivated()
    {
        // 声明了关卡音乐就切换过去，未声明则维持当前曲目
        if (_session.Music is { } music)
            Game.Music.Play(music, TimeSpan.FromSeconds(1));
    }

    public override void Update(GameTime gameTime)
    {
        _session.Update(gameTime);

        if (_autoExitHandled)
            return;
        World.Query(
            new QueryDescription().WithAll<LevelExitState>(),
            (ref LevelExitState state) =>
            {
                if (state.ShouldExit && !_autoExitHandled)
                {
                    var cleared = _viewEntity.Get<LevelClearState>().Status == ClearStatus.Cleared;
                    _autoExitHandled = true;
                    var query = cleared
                        ? new Dictionary<string, object> { ["reveal"] = _levelIndex + 1 }
                        : null;
                    Game.ScreenManager.Backward(query: query);
                }
            }
        );
    }

    public override void Dispose()
    {
        base.Dispose();
        _session.Dispose();
    }
}
