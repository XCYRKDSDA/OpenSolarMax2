using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Xna.Framework.Graphics;
using OneOf;
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
    /// 主条目背景的纹理锚点，以纹理像素计。锁定模式取该模组第一关对应的纹理锚点，
    /// 增量模式取 0；背景为空时为 null
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
    /// 页面背景的滚动配置：锁定模式按位置落位，增量模式按滚动量累加
    /// </summary>
    OneOf<LockedBackgroundScrolling, DeltaBackgroundScrolling> BackgroundScrolling { get; }

    ICommand SelectItemCommand { get; }

    ICommand? BackwardCommand { get; }
}
