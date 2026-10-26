using OneOf;

namespace OpenSolarMax.Game.Modding;

/// <summary>
/// 绝对间距：相邻位置之间的纹理锚点变化量，以纹理像素计
/// </summary>
/// <param name="TextureAnchorPerPosition">每个位置单位对应的纹理锚点变化量，纹理像素</param>
internal readonly record struct AbsoluteSpacing(float TextureAnchorPerPosition);

/// <summary>
/// 相对间距：相邻位置之间的背景位移变化量，以条目间距为单位
/// </summary>
/// <param name="Multiplier">每个位置单位对应的条目间距倍数，null 表示未声明，默认值由 UI 确定</param>
internal readonly record struct RelativeSpacing(float? Multiplier);

/// <summary>
/// 背景滚动配置：指定位置的纹理锚点，其余位置按间距线性外推
/// </summary>
/// <param name="Position">指定的位置</param>
/// <param name="TextureAnchor">指定位置对应的纹理锚点，以纹理像素计</param>
/// <param name="Spacing">
/// 相邻位置之间的间距。绝对间距直接给出纹理像素，相对间距给出条目间距的倍数，
/// 由使用方乘以自己的条目间距
/// </param>
internal readonly record struct BackgroundScrolling(
    float Position,
    float TextureAnchor,
    OneOf<AbsoluteSpacing, RelativeSpacing> Spacing
);
