using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal interface IMenuLikeViewModel : IViewModel
{
    ObservableCollection<string> Items { get; }

    int PrimaryItemIndex { get; set; }

    IFadableImage PrimaryItemPreview { get; }

    Texture2D? PrimaryItemBackground { get; }

    int? SecondaryItemIndex { get; set; }

    IFadableImage? SecondaryItemPreview { get; }

    Texture2D? SecondaryItemBackground { get; }

    Texture2D PageBackground { get; }

    ICommand SelectItemCommand { get; }

    event EventHandler<IMenuLikeViewModel>? NavigateIn;
}
