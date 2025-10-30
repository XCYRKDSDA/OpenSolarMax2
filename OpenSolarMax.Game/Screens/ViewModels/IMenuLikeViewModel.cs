using System.Collections.ObjectModel;
using System.Windows.Input;
using OneOf;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal interface IMenuLikeViewModel : IViewModel
{
    ObservableCollection<string> Items { get; }

    OneOf<int, (int, int)> CurrentIndex { get; set; }

    OneOf<IFadableImage, (IFadableImage, IFadableImage)> CurrentPreview { get; }

    ICommand SelectItemCommand { get; }
}
