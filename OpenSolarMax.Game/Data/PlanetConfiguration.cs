
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Data;

internal class PlanetConfiguration : IEntityConfiguration
{
    /// <summary>
    /// 星球的半径
    /// </summary>
    public float? Radius { get; set; }

    /// <summary>
    /// 星球所在的位置
    /// </summary>
    public Vector2? Position { get; set; }

    public class OrbitConfiguration
    {
        /// <summary>
        /// 星球所围绕的实体
        /// </summary>
        public string? Parent { get; set; }

        /// <summary>
        /// 轨道的形状
        /// </summary>
        public Vector2? Shape { get; set; }

        /// <summary>
        /// 轨道的公转周期
        /// </summary>
        public float? Period { get; set; }

        /// <summary>
        /// 初始时星球在轨道上的相位
        /// </summary>
        public float? Phase { get; set; }
    }

    public OrbitConfiguration? Orbit { get; set; }
}
