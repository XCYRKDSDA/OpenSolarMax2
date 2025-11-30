using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;
using OpenSolarMax.Game.Utils;
using XNAGame = Microsoft.Xna.Framework.Game;
using FmodStudioSystem = FMOD.Studio.System;

namespace OpenSolarMax.Game;

public class SolarMax : XNAGame
{
    private readonly GraphicsDeviceManager _graphics;

    private AssetsManager _globalAssets;

    private FmodStudioSystem _globalFmodSystem;

    private ScreenManager _globalScreenManager;
    private RenderTarget2D _renderTarget;
    private SpriteBatch _spriteBatch;

    public SolarMax()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1920,
            PreferredBackBufferHeight = 1080,
            PreferMultiSampling = true
        };
        _graphics.PreparingDeviceSettings += PreparingDeviceSettings;

        IsMouseVisible = true;

        MyraEnvironment.Game = this;
        MyraEnvironment.SmoothText = true;
        MyraEnvironment.DrawWidgetsFrames = true;
    }

    public FmodStudioSystem FmodSystem => _globalFmodSystem;

    public AssetsManager Assets => _globalAssets;

    public ScreenManager ScreenManager => _globalScreenManager;

    private void PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
    }

    protected override void Initialize()
    {
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            GraphicsDevice.PresentationParameters.BackBufferWidth,
            GraphicsDevice.PresentationParameters.BackBufferHeight,
            false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents // 保留
        );
        _spriteBatch = new SpriteBatch(GraphicsDevice, 1);

        // 初始化全局 fmod 音频系统
        FmodException.Check(FmodStudioSystem.create(out _globalFmodSystem));
        FmodException.Check(_globalFmodSystem.initialize(512, INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, 0));

        // 初始化默认资产加载器
        AssetsManager.RegisterDefaultLoader(new Texture2DLoader(GraphicsDevice));
        AssetsManager.RegisterDefaultLoader(new TextureAtlasLoader());
        AssetsManager.RegisterDefaultLoader(new TextureRegionLoader());
        AssetsManager.RegisterDefaultLoader(new NinePatchRegionLoader());
        AssetsManager.RegisterDefaultLoader(new FontSystemLoader());
        AssetsManager.RegisterDefaultLoader(new ByteArrayLoader());
        AssetsManager.RegisterDefaultLoader(new FmodBankLoader(_globalFmodSystem));
        AssetsManager.RegisterDefaultLoader(new FmodEventLoader(_globalFmodSystem));

        // 初始化全局资产
        _globalAssets = new AssetsManager(Folders.Content);

        // 初始化全局界面管理器
        _globalScreenManager = new ScreenManager(this);
        Components.Add(_globalScreenManager);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        var initializationViewModel = new InitializationViewModel(this);
        var initializationScreen = new InitializationScreen(initializationViewModel, this);
        _globalScreenManager.ActiveScreen = initializationScreen;
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();

        _globalFmodSystem.release();
    }

    protected override void Update(GameTime gameTime)
    {
        _globalFmodSystem.update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);

        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);

        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin();
        _spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();
    }
}
