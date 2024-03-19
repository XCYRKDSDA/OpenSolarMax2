using Microsoft.Xna.Framework;
using OpenSolarMax.Game.System;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 实体在世界坐标系中的位姿和变换
/// </summary>
[Component]
public struct AbsoluteTransform
{
    public Vector3 Translation = Vector3.Zero;

    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    /// 当前坐标系到根坐标系的三维变换
    /// </summary>
    public Matrix TransformToRoot
    {
        readonly get => Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Translation);
        set => value.Decompose(out _, out Rotation, out Translation);
    }

    public AbsoluteTransform() { }

    public AbsoluteTransform(in Vector3 translation, in Quaternion rotation)
    {
        Translation = translation;
        Rotation = rotation;
    }

    public AbsoluteTransform(in Matrix matrix) { matrix.Decompose(out _, out Rotation, out Translation); }

    public override readonly string? ToString() => $"Translation: {Translation}, Rotation: {Rotation}";
}

/// <summary>
/// 实体相对其父实体的位姿和变换
/// </summary>
[Component]
public struct RelativeTransform
{
    public Vector3 Translation = Vector3.Zero;

    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    /// 当前坐标系到父坐标系的三维变换
    /// </summary>
    public Matrix TransformToParent
    {
        readonly get => Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Translation);
        set => value.Decompose(out _, out Rotation, out Translation);
    }

    public RelativeTransform() { }

    public RelativeTransform(in Vector3 translation, in Quaternion rotation)
    {
        Translation = translation;
        Rotation = rotation;
    }

    public RelativeTransform(in Matrix matrix) { matrix.Decompose(out _, out Rotation, out Translation); }

    public override readonly string? ToString() => $"Translation: {Translation}, Rotation: {Rotation}";
}
