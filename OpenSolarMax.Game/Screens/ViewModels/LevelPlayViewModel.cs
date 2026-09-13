using System.Runtime.InteropServices;
using System.Windows.Input;
using Arch.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelPlayViewModel : ViewModelBase
{
    private readonly LevelSession _session;
    private readonly Entity _viewEntity;

    [ObservableProperty]
    private bool _paused = false;

    [ObservableProperty]
    private float _simulateSpeed = 1;

    [ObservableProperty]
    private Texture2D _background;

    [ObservableProperty]
    private ICommand _exitCommand;

    public World World => _session.World;

    public AggregateSystem InputSystem => _session.InputSystems;

    public AggregateSystem RenderSystem => _session.RenderSystems;

    public Entity ViewEntity => _viewEntity;

    public LevelPlayViewModel(LevelSession session, Texture2D background, SolarMax game)
        : base(game)
    {
        // 记录会话
        _session = session;
        _background = background;

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

    public override void Update(GameTime gameTime)
    {
        _session.Update(gameTime);
    }

    public override void Dispose()
    {
        base.Dispose();
        _session.Dispose();
    }
}
