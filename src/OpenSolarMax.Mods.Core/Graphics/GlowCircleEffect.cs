using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class GlowCircleEffect : Effect, IEffectMatrices
{
    #region Effect Parameters

    private readonly EffectParameter _toNdcParam;

    private readonly EffectParameter _centerParam;
    private readonly EffectParameter _radiusParam;
    private readonly EffectParameter _innerRadiusParam;

    #endregion

    #region Fields

    private Matrix _world = Matrix.Identity;
    private Matrix _view = Matrix.Identity;
    private Matrix _proj = Matrix.Identity;

    private Vector2 _center = Vector2.Zero;
    private float _radius = 0;
    private float _innerRadius = 0;

    [Flags]
    private enum DirtyFlags
    {
        None = 0,
        WorldViewProj = 1 << 1,
        Center = 1 << 2,
        Radius = 1 << 3,
        InnerRadius = 1 << 4,
        All = -1,
    }

    private DirtyFlags _dirtyFlags = DirtyFlags.All;

    #endregion

    #region Properties

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

    public float InnerRadius
    {
        get => _innerRadius;
        set
        {
            _innerRadius = value;
            _dirtyFlags |= DirtyFlags.InnerRadius;
        }
    }

    #endregion

    public GlowCircleEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, EffectResource.GlowCircleEffect.Bytecode)
    {
        _toNdcParam = Parameters["to_ndc"];
        _centerParam = Parameters["center"];
        _radiusParam = Parameters["radius"];
        _innerRadiusParam = Parameters["inner_radius"];
    }

    protected override void OnApply()
    {
        if ((_dirtyFlags & DirtyFlags.WorldViewProj) != DirtyFlags.None)
            _toNdcParam.SetValue(_world * _view * _proj);

        if ((_dirtyFlags & DirtyFlags.Center) != DirtyFlags.None)
            _centerParam.SetValue(_center);

        if ((_dirtyFlags & DirtyFlags.Radius) != DirtyFlags.None)
            _radiusParam.SetValue(_radius);

        if ((_dirtyFlags & DirtyFlags.InnerRadius) != DirtyFlags.None)
            _innerRadiusParam.SetValue(_innerRadius);

        _dirtyFlags = DirtyFlags.None;
    }
}
