using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class SegmentEffect : Effect, IEffectMatrices
{
    #region Effect Parameters

    private readonly EffectParameter _toNdcParam;

    private readonly EffectParameter _headParam, _tailParam;
    private readonly EffectParameter _thicknessParam;

    #endregion

    #region Fields

    private Matrix _world = Matrix.Identity;
    private Matrix _view = Matrix.Identity;
    private Matrix _proj = Matrix.Identity;

    private Vector2 _head = Vector2.Zero, _tail = Vector2.Zero;
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

    public Vector2 Head
    {
        get => _head;
        set { _head = value; _dirtyFlags |= DirtyFlags.Shape; }
    }

    public Vector2 Tail
    {
        get => _tail;
        set { _tail = value; _dirtyFlags |= DirtyFlags.Shape; }
    }

    public float Thickness
    {
        get => _thickness;
        set { _thickness = value; _dirtyFlags |= DirtyFlags.Thickness; }
    }

    #endregion

    public SegmentEffect(GraphicsDevice graphicsDevice, IAssetsManager assets)
        : base(graphicsDevice, assets.Load<byte[]>("Effects/Segment.mgfxo"))
    {
        _toNdcParam = Parameters["to_ndc"];
        _headParam = Parameters["head"];
        _tailParam = Parameters["tail"];
        _thicknessParam = Parameters["thickness"];
    }

    protected override void OnApply()
    {
        if ((_dirtyFlags & DirtyFlags.WorldViewProj) != DirtyFlags.None)
            _toNdcParam.SetValue(_world * _view * _proj);

        if ((_dirtyFlags & DirtyFlags.Shape) != DirtyFlags.None)
        {
            _headParam.SetValue(_head);
            _tailParam.SetValue(_tail);
        }

        if ((_dirtyFlags & DirtyFlags.Thickness) != DirtyFlags.None)
            _thicknessParam.SetValue(_thickness);

        _dirtyFlags = DirtyFlags.None;
    }
}
