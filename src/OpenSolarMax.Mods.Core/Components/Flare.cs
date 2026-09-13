using Nine.Graphics;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 天体绽放特效组件。注入到所有 gameplay 天体上，纹理来自天体 Shape 纹理。
/// </summary>
internal struct Flare()
{
    /// <summary>
    /// 绽放特效使用的纹理
    /// </summary>
    public TextureRegion Texture = null!;
}
