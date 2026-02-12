using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using Arch.Buffer;
using Arch.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelsViewModel : ViewModelBase, IMenuLikeViewModel
{
    private readonly LevelModContext _levelModContext;

    private readonly List<LevelFile> _levels;
    private readonly List<IFadableImage> _previews;
    private readonly List<AggregateSystem> _previewSystems;
    private readonly List<World> _worlds;

    [ObservableProperty]
    private ObservableCollection<string> _items;

    [ObservableProperty]
    private Texture2D _pageBackground;

    [ObservableProperty]
    private Texture2D? _primaryItemBackground;

    [ObservableProperty]
    private int _primaryItemIndex;

    [ObservableProperty]
    private IFadableImage _primaryItemPreview;

    [ObservableProperty]
    private Texture2D? _secondaryItemBackground;

    [ObservableProperty]
    private int? _secondaryItemIndex;

    [ObservableProperty]
    private IFadableImage? _secondaryItemPreview;

    [ObservableProperty]
    private ICommand _selectItemCommand;

    public LevelsViewModel(LevelModInfo levelModInfo, SolarMax game, IProgress<float>? progress = null) : base(game)
    {
        progress?.Report(0);

        // 设置基础内容。该步骤占 10%

        _items = [];
        _levels = [];
        _worlds = [];
        _previewSystems = [];
        _previews = [];
        _selectItemCommand = new RelayCommand<int>(OnSelectItem);

        // 加载关卡模组上下文

        _levelModContext = new LevelModContext(levelModInfo, game);

        // 加载所有关卡

        var factory = new ConceptFactory(_levelModContext.ConceptInfos.Values, new Dictionary<Type, object>()
        {
            [typeof(GraphicsDevice)] = game.GraphicsDevice,
            [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
        });
        var worldLoader = new WorldLoader(
            factory, _levelModContext.ConfigurationSchemaInfos.ToDictionary(p => p.Key, p => p.Value.ConceptName)
        );
        var levelLoader = new LevelLoader(_levelModContext.ConfigurationSchemaInfos);

        // 目前假设所有关卡平铺在 Levels 目录下
        foreach (var levelFile in levelModInfo.Levels.EnumerateFiles("*.json"))
        {
            var level = levelLoader.Load(levelFile.FileSystem, _levelModContext.LocalAssets, levelFile.Path);
            _levels.Add(level);

            // 加载关卡内容
            var world = World.Create();
            var simulateSystem = new AggregateSystem(
                world, _levelModContext.SystemTypes.Simulate.Sorted,
                new Dictionary<Type, object>
                {
                    [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                    [typeof(IConceptFactory)] = factory,
                    [typeof(IConfiguration)] = _levelModContext.LocalConfigs,
                },
                _levelModContext.HookImplMethods.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MethodInfo>)
            );
            var commandBuffer = new CommandBuffer();
            var enumerator = worldLoader.LoadStepByStep(level, world, commandBuffer);
            while (enumerator.MoveNext())
            {
                commandBuffer.Playback(world);
                simulateSystem.LateUpdate();
            }
            _worlds.Add(world);

            // 构造预览系统
            var previewSystem = new AggregateSystem(
                world, _levelModContext.SystemTypes.Preview.Sorted,
                new Dictionary<Type, object>
                {
                    [typeof(GraphicsDevice)] = game.GraphicsDevice,
                    [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                },
                _levelModContext.HookImplMethods.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MethodInfo>)
            );
            _previewSystems.Add(previewSystem);

            // 添加元素
            _items.Add(levelFile.Name);
            _previews.Add(new FadableRichText(new RichTextLayout()
            {
                Text = levelFile.Name,
                Font = _levelModContext.LocalAssets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
            }));
        }

        // 移动到默认位置
        _primaryItemIndex = 0;
        _primaryItemPreview = new WorldRenderer(_worlds[0], _previewSystems[0], game.GraphicsDevice);
        _primaryItemBackground = null;
        _secondaryItemIndex = null;
        _secondaryItemPreview = null;
        _secondaryItemBackground = null;
    }

    public event EventHandler<IViewModel>? NavigateIn;

    partial void OnPrimaryItemIndexChanged(int value)
    {
        PrimaryItemPreview = new WorldRenderer(_worlds[value], _previewSystems[value], Game.GraphicsDevice);
    }

    partial void OnSecondaryItemIndexChanged(int? value)
    {
        SecondaryItemPreview =
            value is null
                ? null
                : new WorldRenderer(_worlds[value.Value], _previewSystems[value.Value], Game.GraphicsDevice);
    }

    private void OnSelectItem(int idx)
    {
        var levelPlayViewModel = new LevelPlayViewModel(_levels[idx], _levelModContext, Game);
        NavigateIn?.Invoke(this, levelPlayViewModel);
    }
}
