using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nine.Assets;
using OpenSolarMax.Core;
using OpenSolarMax.Core.Components;
using OpenSolarMax.Core.Systems;
using OpenSolarMax.Core.Utils;
using OpenSolarMax.Game.Assets;
using OpenSolarMax.Game.Data;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Game;

public class SolarMax : XNAGame
{
    private readonly GraphicsDeviceManager _graphics;

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
    }

    private void PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
    }

    private readonly World _world = World.Create();
    private readonly Arch.System.Group<GameTime> _systems = new();
    private readonly Arch.System.Group<GameTime> _uiSystems = new();

    protected override void LoadContent()
    {
        var assets = new AssetsManager(new ResourceFileSystem(Assembly.GetExecutingAssembly(), "Content"));
        assets.RegisterLoader(new Texture2DLoader(GraphicsDevice));
        assets.RegisterLoader(new TextureAtlasLoader());
        assets.RegisterLoader(new TextureRegionLoader());
        assets.RegisterLoader(new LevelLoader()
        {
            ConfigurationTypes =
            {
                { "empty", [typeof(EmptyObjectConfiguration)] },
                { "planet", [typeof(PlanetConfiguration)] },
                { "ship", [typeof(ShipConfiguration)] },
            }
        });

        var level = assets.Load<Level>("Levels/Test.json");

        var worldLoader = new WorldLoader();
        worldLoader.RegisterConfigurator(new EmptyObjectConfigurator());
        worldLoader.RegisterConfigurator(new PlanetConfigurator(assets));
        worldLoader.RegisterConfigurator(new ShipConfigurator(assets));

        worldLoader.Load(level, _world);

        var camera = _world.Construct(in Archetypes.Camera);

        camera.Get<Camera>() = new()
        {
            Width = 1920,
            Height = 1080,
            ZFar = 1000,
            ZNear = -1000,
            Output = new(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height)
        };

        _systems.Add(
            new RevolveEntitiesAroundOrbitsSystem(_world),
            new CalculateAbsoluteTransformSystem(_world)
        );

        _uiSystems.Add(
            new DrawSpritesSystem(_world, GraphicsDevice)
        );
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _systems.BeforeUpdate(in gameTime);
        _systems.Update(in gameTime);
        _systems.AfterUpdate(in gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _uiSystems.Update(in gameTime);

        base.Draw(gameTime);
    }
}
