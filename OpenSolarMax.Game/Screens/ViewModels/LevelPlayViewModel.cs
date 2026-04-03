using System.Runtime.InteropServices;
using Arch.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelPlayViewModel : ViewModelBase
{
    private readonly LevelRuntime _runtime;
    private readonly Entity _viewEntity;
    private readonly GameTime _playTime = new();

    [ObservableProperty]
    private bool _paused = false;

    [ObservableProperty]
    private float _simulateSpeed = 1;

    [ObservableProperty]
    private Texture2D _background;

    public World World => _runtime.World;

    public AggregateSystem RenderSystem => _runtime.RenderSystems;

    public Entity ViewEntity => _viewEntity;

    public LevelPlayViewModel(LevelRuntime levelRuntime, Texture2D background, SolarMax game)
        : base(game)
    {
        // 记录运行时
        _runtime = levelRuntime;
        _background = background;

        // 查找相机
        var viewDesc = new QueryDescription().WithAll<ViewTag>();
        var viewCount = _runtime.World.CountEntities(in viewDesc);
        if (viewCount > 1)
            throw new Exception("there're more than one view entities in the world!");
        if (viewCount <= 0)
            throw new Exception("there's no view entity in the world!");
        _runtime.World.GetEntities(in viewDesc, MemoryMarshal.CreateSpan(ref _viewEntity, 1));

        // 设置 fmod 系统
        _runtime.World.Query(
            new QueryDescription().WithAll<FMOD.Studio.System>(),
            (ref FMOD.Studio.System fmodSystem) => fmodSystem = game.FmodSystem
        );
    }

    public override void Update(GameTime gameTime)
    {
        if (Paused)
            return;

        // 更新时间
        _playTime.ElapsedGameTime = gameTime.ElapsedGameTime * SimulateSpeed;
        _playTime.TotalGameTime += _playTime.ElapsedGameTime;
        _playTime.IsRunningSlowly = gameTime.IsRunningSlowly;

        // 更新世界
        _runtime.InputSystems.Update(_playTime);
        _runtime.AiSystems.Update(_playTime);
        _runtime.SimulateSystems.Update(_playTime);
    }
}
