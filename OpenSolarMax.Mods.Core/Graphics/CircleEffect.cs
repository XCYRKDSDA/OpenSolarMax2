using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class CircleEffect : Effect, IEffectMatrices
{
    #region Effect Parameters

    private readonly EffectParameter _toNdcParam;

    private readonly EffectParameter _centerParam;
    private readonly EffectParameter _radiusParam;
    private readonly EffectParameter _thicknessParam;

    #endregion

    #region Fields

    private Matrix _world = Matrix.Identity;
    private Matrix _view = Matrix.Identity;
    private Matrix _proj = Matrix.Identity;

    private Vector2 _center = Vector2.Zero;
    private float _radius = 0;
    private float _thickness = 0;

    [Flags]
    private enum DirtyFlags
    {
        None = 0,
        WorldViewProj = 1 << 1,
        Center = 1 << 2,
        Radius = 1 << 3,
        Thickness = 1 << 4,
        All = -1
    }

    private DirtyFlags _dirtyFlags = DirtyFlags.All;

    #endregion

    #region Proporties

    public Matrix World
    {
        get => _world;
        set
        {
            _world = value;
            _dirtyFlags |= DirtyFlags.WorldViewProj;
        }
    }

    public Matrix View
    {
        get => _view;
        set
        {
            _view = value;
            _dirtyFlags |= DirtyFlags.WorldViewProj;
        }
    }

    public Matrix Projection
    {
        get => _proj;
        set
        {
            _proj = value;
            _dirtyFlags |= DirtyFlags.WorldViewProj;
        }
    }

    public Vector2 Center
    {
        get => _center;
        set
        {
            _center = value;
            _dirtyFlags |= DirtyFlags.Center;
        }
    }

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            _dirtyFlags |= DirtyFlags.Radius;
        }
    }

    public float Thickness
    {
        get => _thickness;
        set
        {
            _thickness = value;
            _dirtyFlags |= DirtyFlags.Thickness;
        }
    }

    #endregion

    public CircleEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, EffectResource.CircleEffect.Bytecode)
    {
        _toNdcParam = Parameters["to_ndc"];
        _centerParam = Parameters["center"];
        _radiusParam = Parameters["radius"];
        _thicknessParam = Parameters["thickness"];
    }

    protected override void OnApply()
    {
        if ((_dirtyFlags & DirtyFlags.WorldViewProj) != DirtyFlags.None)
            _toNdcParam.SetValue(_world * _view * _proj);

        if ((_dirtyFlags & DirtyFlags.Center) != DirtyFlags.None)
            _centerParam.SetValue(_center);

        if ((_dirtyFlags & DirtyFlags.Radius) != DirtyFlags.None)
            _radiusParam.SetValue(_radius);

        if ((_dirtyFlags & DirtyFlags.Thickness) != DirtyFlags.None)
            _thicknessParam.SetValue(_thickness);

        _dirtyFlags = DirtyFlags.None;
    }
}
