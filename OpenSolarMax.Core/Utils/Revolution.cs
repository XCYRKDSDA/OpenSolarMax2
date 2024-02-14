using Microsoft.Xna.Framework;
using OpenSolarMax.Core.Components;

namespace OpenSolarMax.Core.Utils;
public static class Revolution
{
    /// <summary>
    /// 计算实体绕其所在轨道公转的相对位姿
    /// </summary>
    /// <param name="orbit">实体所在轨道</param>
    /// <param name="state">实体当前公转状态</param>
    /// <returns>单位相对轨道所在实体的相对变换</returns>
    public static RelativeTransform CalculateTransform(in RevolutionOrbit orbit, in RevolutionState state) =>
        // 以+Z轴为轴, 逆时针旋转
        new(Matrix.CreateTranslation(orbit.Shape.Width, 0, 0)
            * Matrix.CreateRotationZ(state.Phase)
            * Matrix.CreateScale(1, orbit.Shape.Height / orbit.Shape.Width, 1)
            * Matrix.CreateFromQuaternion(orbit.Rotation));
}
