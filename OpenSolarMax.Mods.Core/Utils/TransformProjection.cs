using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Utils;

/// <summary>
/// 负责在 2D 坐标系和 3D 坐标系之间转换的工具方法
/// </summary>
public static class TransformProjection
{
    /// <summary>
    /// 将 2D 向量转换为 3D 向量。转换后的 3D 向量 Z 轴为 0
    /// </summary>
    /// <param name="vector">XY 平面上的 2D 向量</param>
    /// <returns>Z 轴为 0 的 3D 向量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 To3D(Vector2 vector) => new(vector, 0);

    /// <summary>
    /// 将 XY 平面上的旋转转换为 3D 旋转四元数格式。转换后的 3D 旋转将严格位于 XY 平面上
    /// </summary>
    /// <param name="rotation">XY 平面上从 X 轴逆时针旋转的弧度</param>
    /// <returns>XY 平面上的 3D 旋转</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion To3D(float rotation) =>
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation);

    /// <summary>
    /// 将 2D 坐标和旋转转换为 3D 坐标和旋转。转换后的 3D 位姿将严格位于 XY 平面上。<br/>
    /// 该方法本质上为结合 <see cref="To3D(Vector2)"/> 和 <see cref="To3D(float)"/> 的语法糖
    /// </summary>
    /// <param name="position">XY 平面上的 2D 坐标</param>
    /// <param name="rotation">XY 平面上从 X 轴逆时针旋转的弧度</param>
    /// <returns>3D 坐标系中的位姿</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector3, Quaternion) To3D(Vector2 position, float rotation) =>
        (To3D(position), To3D(rotation));

    /// <summary>
    /// 将 3D 向量投影到 2D XY 平面上
    /// </summary>
    /// <param name="vector">3D 向量</param>
    /// <returns>XY 平面上的 2D 向量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 To2D(Vector3 vector) => new(vector.X, vector.Y);

    /// <summary>
    /// 将 3D 旋转投影到 2D XY 平面上
    /// </summary>
    /// <param name="rotation">3D 旋转的四元数</param>
    /// <returns>XY 平面上从 X 轴逆时针旋转的弧度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float To2D(Quaternion rotation)
    {
        // 获取经过 3D 旋转后的 X 方向向量
        var rotatedUnitX = Vector3.Transform(Vector3.UnitX, Matrix.CreateFromQuaternion(rotation));
        // 投影方向向量
        var projectedUnitX = To2D(rotatedUnitX);
        // 计算投影后的旋转
        return MathF.Atan2(projectedUnitX.Y, projectedUnitX.X);
    }

    /// <summary>
    /// 将 3D 位姿投影到 2D XY 平面上。<br/>
    /// 该方法本质上为结合 <see cref="To2D(Vector3)"/> 和 <see cref="To2D(Quaternion)"/> 的语法糖
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public static (Vector2, float) To2D(Vector3 position, Quaternion rotation) =>
        (To2D(position), To2D(rotation));

    /// <summary>
    /// 使位姿的 X 轴指向目标方向，同时将 Z 轴约束在 X 轴与世界 Z 轴构成的平面内，从而让贴图尽可能面向世界 XY 平面
    /// </summary>
    /// <param name="vector">姿态 X 轴必须指向的方向</param>
    /// <returns>一个 3D 旋转四元数，使姿态尽量面向 XY 平面</returns>
    public static Quaternion UprightAim(Vector3 vector)
    {
        var unitX = Vector3.Normalize(vector);
        var unitY = Vector3.Normalize(new Vector3(-unitX.Y, unitX.X, 0));
        var unitZ = Vector3.Cross(unitX, unitY);
        var rotation = new Matrix
        {
            Right = unitX,
            Up = unitY,
            Backward = unitZ,
        };
        return Quaternion.CreateFromRotationMatrix(rotation);
    }
}
