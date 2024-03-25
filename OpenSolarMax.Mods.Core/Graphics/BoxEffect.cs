using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class BoxEffect : Effect, IEffectMatrices
{
    #region Effect Parameters

    private readonly EffectParameter _toNdcParam;

    private readonly EffectParameter _originParam, _sizeParam;
    private readonly EffectParameter _thicknessParam;

    #endregion

    #region Fields

    private Matrix _world = Matrix.Identity;
    private Matrix _view = Matrix.Identity;
    private Matrix _proj = Matrix.Identity;

    private Vector2 _origin = Vector2.Zero, _size = Vector2.Zero;
    private float _thickness = 0;

    [Flags]
    private enum DirtyFlags
    {
        None = 0,
        WorldViewProj = 1 << 1,
        Shape = 1 << 2,
        Thickness = 1 << 3,
        All = -1
    }

    private DirtyFlags _dirtyFlags = DirtyFlags.All;

    #endregion

    #region Proporties

    public Matrix World
    {
        get => _world;
        set { _world = value; _dirtyFlags |= DirtyFlags.WorldViewProj; }
    }

    public Matrix View
    {
        get => _view;
        set { _view = value; _dirtyFlags |= DirtyFlags.WorldViewProj; }
    }

    public Matrix Projection
    {
        get => _proj;
        set { _proj = value; _dirtyFlags |= DirtyFlags.WorldViewProj; }
    }

    public Vector2 Origin
    {
        get => _origin;
        set { _origin = value; _dirtyFlags |= DirtyFlags.Shape; }
    }

    public Vector2 Size
    {
        get => _size;
        set { _size = value; _dirtyFlags |= DirtyFlags.Shape; }
    }

    public Rectangle Shape
    {
        get => new(_origin.ToPoint(), _size.ToPoint());
        set { _origin = value.Location.ToVector2(); _size = value.Size.ToVector2(); _dirtyFlags |= DirtyFlags.Shape; }
    }

    public float Thickness
    {
        get => _thickness;
        set { _thickness = value; _dirtyFlags |= DirtyFlags.Thickness; }
    }

    #endregion

    public BoxEffect(GraphicsDevice graphicsDevice, IAssetsManager assets)
        : base(graphicsDevice, assets.Load<byte[]>("Effects/Box.mgfxo"))
    {
        _toNdcParam = Parameters["to_ndc"];
        _originParam = Parameters["origin"];
        _sizeParam = Parameters["size"];
        _thicknessParam = Parameters["thickness"];
    }

    protected override void OnApply()
    {
        if ((_dirtyFlags & DirtyFlags.WorldViewProj) != DirtyFlags.None)
            _toNdcParam.SetValue(_world * _view * _proj);

        if ((_dirtyFlags & DirtyFlags.Shape) != DirtyFlags.None)
        {
            _originParam.SetValue(_origin);
            _sizeParam.SetValue(_size);
        }

        if ((_dirtyFlags & DirtyFlags.Thickness) != DirtyFlags.None)
            _thicknessParam.SetValue(_thickness);

        _dirtyFlags = DirtyFlags.None;
    }
}
