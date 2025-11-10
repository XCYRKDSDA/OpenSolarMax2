using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using OneOf;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal interface IMenuLikeViewModel : IViewModel
{
    ObservableCollection<string> Items { get; }

    OneOf<int, (int, int)> CurrentIndex { get; set; }

    OneOf<IFadableImage, (IFadableImage, IFadableImage)> CurrentPreview { get; }

    OneOf<Texture2D?, (Texture2D?, Texture2D?)> CurrentBackground { get; }

    Texture2D Background { get; }

    ICommand SelectItemCommand { get; }

    event EventHandler<IMenuLikeViewModel>? NavigateIn;
}
