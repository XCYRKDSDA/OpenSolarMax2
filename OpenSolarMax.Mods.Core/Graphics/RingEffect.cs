using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class RingEffect : Effect, IEffectMatrices
{
    #region Effect Parameters

    private readonly EffectParameter _toNdcParam;

    private readonly EffectParameter _centerParam;
    private readonly EffectParameter _radiusParam;
    private readonly EffectParameter _thicknessParam;

    private readonly EffectParameter _inferiorParam;
    private readonly EffectParameter _headVectorParam;
    private readonly EffectParameter _tailVectorParam;

    #endregion

    #region Fields

    private Matrix _world = Matrix.Identity;
    private Matrix _view = Matrix.Identity;
    private Matrix _proj = Matrix.Identity;

    private Vector2 _center = Vector2.Zero;
    private float _radius = 0;
    private float _thickness = 0;

    private float _head = 0, _radians = 0;

    [Flags]
    private enum DirtyFlags
    {
        None = 0,
        WorldViewProj = 1 << 1,
        Center = 1 << 2,
        Radius = 1 << 3,
        Thickness = 1 << 4,
        ArcRange = 1 << 5,
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

    public float Head
    {
        get => _head;
        set
        {
            _head = value;
            _dirtyFlags |= DirtyFlags.ArcRange;
        }
    }

    public float Radians
    {
        get => _radians;
        set
        {
            _radians = value;
            _dirtyFlags |= DirtyFlags.ArcRange;
        }
    }

    #endregion

    public RingEffect(GraphicsDevice graphicsDevice, IAssetsManager assets)
        : base(graphicsDevice, assets.Load<byte[]>("Effects/Ring.mgfxo"))
    {
        _toNdcParam = Parameters["to_ndc"];
        _centerParam = Parameters["center"];
        _radiusParam = Parameters["radius"];
        _thicknessParam = Parameters["thickness"];
        _inferiorParam = Parameters["inferior"];
        _headVectorParam = Parameters["head_vector"];
        _tailVectorParam = Parameters["tail_vector"];
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

        if ((_dirtyFlags & DirtyFlags.ArcRange) != DirtyFlags.None)
        {
            // 统一方向
            if (_radians < 0)
            {
                _head += _radians;
                _radians *= -1;
            }

            // 归一化角度
            _head %= 2 * MathF.PI;
            _radians %= 2 * MathF.PI;

            Vector2 headVec, tailVec;
            (headVec.Y, headVec.X) = MathF.SinCos(_head);
            (tailVec.Y, tailVec.X) = MathF.SinCos(_head + _radians);

            _headVectorParam.SetValue(headVec);
            _tailVectorParam.SetValue(tailVec);
            _inferiorParam.SetValue(_radians < MathF.PI);
        }

        _dirtyFlags = DirtyFlags.None;
    }
}
