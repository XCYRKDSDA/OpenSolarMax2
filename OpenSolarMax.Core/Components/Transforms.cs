using Microsoft.Xna.Framework;

namespace OpenSolarMax.Core.Components;

/// <summary>
/// 实体在世界坐标系中的位姿和变换
/// </summary>
public struct AbsoluteTransform
{
    private Vector3 _translation = Vector3.Zero;

    private Quaternion _rotation = Quaternion.Identity;

    public Vector3 Translation { readonly get => _translation; set => _translation = value; }

    public Quaternion Rotation { readonly get => _rotation; set => _rotation = value; }

    /// <summary>
    /// 当前坐标系到根坐标系的三维变换
    /// </summary>
    public Matrix TransformToRoot
    {
        readonly get => Matrix.CreateFromQuaternion(_rotation) * Matrix.CreateTranslation(_translation);
        set => value.Decompose(out _, out _rotation, out _translation);
    }

    public AbsoluteTransform() { }

    public AbsoluteTransform(in Vector3 translation, in Quaternion rotation)
    {
        _translation = translation;
        _rotation = rotation;
    }

    public AbsoluteTransform(in Matrix matrix) { matrix.Decompose(out _, out _rotation, out _translation); }

    public override readonly string? ToString() => $"Translation: {Translation}, Rotation: {Rotation}";
}

/// <summary>
/// 实体相对其父实体的位姿和变换
/// </summary>
public struct RelativeTransform
{
    private Vector3 _translation = Vector3.Zero;

    private Quaternion _rotation = Quaternion.Identity;

    public Vector3 Translation { readonly get => _translation; set => _translation = value; }

    public Quaternion Rotation { readonly get => _rotation; set => _rotation = value; }

    /// <summary>
    /// 当前坐标系到父坐标系的三维变换
    /// </summary>
    public Matrix TransformToParent
    {
        readonly get => Matrix.CreateFromQuaternion(_rotation) * Matrix.CreateTranslation(_translation);
        set => value.Decompose(out _, out _rotation, out _translation);
    }

    public RelativeTransform() { }

    public RelativeTransform(in Vector3 translation, in Quaternion rotation)
    {
        _translation = translation;
        _rotation = rotation;
    }

    public RelativeTransform(in Matrix matrix) { matrix.Decompose(out _, out _rotation, out _translation); }

    public override readonly string? ToString() => $"Translation: {Translation}, Rotation: {Rotation}";
}
