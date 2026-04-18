using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Nine.Animations;
using OpenSolarMax.Game.Screens.Pages;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.UI;
using Svg;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class InitializationViewModel : ViewModelBase, ILoaderViewModel
{
    [ObservableProperty]
    private float _progress = 0;

    [ObservableProperty]
    private bool _loadCompleted = false;

    [ObservableProperty]
    private ICommand _startLoadingCommand;

    private Task<MainMenuPageContext>? _levelPreviewsLoadTask;

    public InitializationViewModel(SolarMax game)
        : base(game)
    {
        _startLoadingCommand = new RelayCommand(OnStartLoading);
    }

    private void OnStartLoading()
    {
        _levelPreviewsLoadTask = Task.Factory.StartNew(
            () => Load(new Progress<float>(v => Progress = v)),
            CancellationToken.None,
            TaskCreationOptions.None,
            Game.BackgroundScheduler
        );
    }

    private static MainMenuPageContext Load(IProgress<float> progress)
    {
        var levelModInfos = Modding.Modding.ListLevelMods();
        var previewableLevelMods = levelModInfos.Select(info =>
        {
            // TODO: 若未指定预览文件则加载缺省图片

            // 加载预览
            using var previewStream = info.Preview!.Open(
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            var previewExtension = info.Preview!.ExtensionWithDot;
            IImage preview = previewExtension switch
            {
                ".png" => new TextureRegion(
                    Texture2D.FromStream(
                        MyraEnvironment.GraphicsDevice,
                        previewStream,
                        DefaultColorProcessors.PremultiplyAlpha
                    )
                ),
                ".svg" => new SvgMyraImage(
                    MyraEnvironment.GraphicsDevice,
                    SvgDocument.Open<SvgDocument>(previewStream)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(previewExtension)),
            };
            var fadablePreview = new FadableWrapper(preview);

            // 加载背景
            Texture2D? background = null;
            if (info.Background is { } backgroundFile)
            {
                using var backgroundStream = backgroundFile.Open(
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );
                background = Texture2D.FromStream(
                    MyraEnvironment.GraphicsDevice,
                    backgroundStream,
                    DefaultColorProcessors.PremultiplyAlpha
                );
            }

            return new PreviewableLevelMod(info, fadablePreview, background);
        });
        return new MainMenuPageContext([.. previewableLevelMods]);
    }

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) =>
            x switch
            {
                < 0 => 0,
                > 1 => 1,
                _ => x * x,
            };
    }

    public override void Update(GameTime gameTime)
    {
        if (
            !LoadCompleted
            && _levelPreviewsLoadTask is not null
            && _levelPreviewsLoadTask.IsCompleted
        )
        {
            LoadCompleted = true;
            Game.ScreenManager.Forward(typeof(MainMenuPage), _levelPreviewsLoadTask.Result);
        }
    }
}
