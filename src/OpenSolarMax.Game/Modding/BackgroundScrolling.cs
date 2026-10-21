using OneOf;

namespace OpenSolarMax.Game.Modding;

/// <summary>
/// 绝对间距：相邻位置之间的目标居中点变化量，以纹理像素计
/// </summary>
/// <param name="TargetFocusXPerPosition">每个位置单位对应的目标居中点变化量，纹理像素</param>
internal readonly record struct AbsoluteSpacing(float TargetFocusXPerPosition);

/// <summary>
/// 相对间距：相邻位置之间的背景位移变化量，以条目间距为单位
/// </summary>
/// <param name="Multiplier">每个位置单位对应的条目间距倍数，null 表示未声明，默认值由 UI 确定</param>
internal readonly record struct RelativeSpacing(float? Multiplier);

/// <summary>
/// 锁定模式的背景滚动配置：指定位置的目标居中点，其余位置按间距线性外推
/// </summary>
/// <param name="Position">指定的位置</param>
/// <param name="TargetFocusX">指定位置对应的目标居中点，以纹理像素计</param>
/// <param name="Spacing">
/// 相邻位置之间的间距。绝对间距直接给出纹理像素，相对间距给出条目间距的倍数，
/// 由使用方乘以自己的条目间距
/// </param>
internal readonly record struct LockedBackgroundScrolling(
    float Position,
    float TargetFocusX,
    OneOf<AbsoluteSpacing, RelativeSpacing> Spacing
);

/// <summary>
/// 增量模式的背景滚动配置：位移增量乘以增量位移倍率后累加到目标背景位移
/// </summary>
/// <param name="DeltaScale">增量位移倍率，null 表示未声明，默认值由 UI 确定</param>
internal readonly record struct DeltaBackgroundScrolling(float? DeltaScale);
