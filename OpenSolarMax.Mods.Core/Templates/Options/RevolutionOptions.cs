using Arch.Core;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Templates.Options;

public class RevolutionOptions
{
    /// <summary>
    /// 所围绕的实体
    /// </summary>
    public required EntityReference Parent { get; set; }

    /// <summary>
    /// 轨道的形状
    /// </summary>
    public required Vector2 Shape { get; set; }

    /// <summary>
    /// 轨道的公转周期
    /// </summary>
    public required float Period { get; set; }

    /// <summary>
    /// 轨道的偏转
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>
    /// 初始时实体在轨道上的相位
    /// </summary>
    public float InitPhase { get; set; } = 0;
}
