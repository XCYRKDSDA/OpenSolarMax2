using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal interface IMenuLikeViewModel : IViewModel
{
    int InitializeIndex { get; }

    ObservableCollection<string> Items { get; }

    int PrimaryItemIndex { get; set; }

    IFadableImage PrimaryItemPreview { get; }

    Texture2D? PrimaryItemBackground { get; }

    /// <summary>
    /// 主条目背景的纹理锚点，以纹理像素计。背景为空时为 null
    /// </summary>
    float? PrimaryItemBackgroundTextureAnchor { get; }

    int? SecondaryItemIndex { get; set; }

    IFadableImage? SecondaryItemPreview { get; }

    Texture2D? SecondaryItemBackground { get; }

    /// <summary>
    /// 次条目背景的纹理锚点，规则同 <see cref="PrimaryItemBackgroundTextureAnchor"/>，背景为空时为 null
    /// </summary>
    float? SecondaryItemBackgroundTextureAnchor { get; }

    Texture2D PageBackground { get; }

    /// <summary>
    /// 页面背景的滚动配置：按指定位置的纹理锚点取值，其余位置按间距线性外推
    /// </summary>
    BackgroundScrolling BackgroundScrolling { get; }

    ICommand SelectItemCommand { get; }

    ICommand? BackwardCommand { get; }
}
