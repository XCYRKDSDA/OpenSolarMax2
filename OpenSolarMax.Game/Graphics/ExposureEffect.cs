using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Game.Graphics;

internal class ExposureEffect : Effect
{
    #region Effect Parameters

    private readonly EffectParameter _textureParam;

    private readonly EffectParameter _centerParam;
    private readonly EffectParameter _halfLifeParam;
    private readonly EffectParameter _amountParam;

    #endregion

    #region Fields

    private Texture? _texture = null;

    private Vector2 _center = Vector2.Zero;
    private float _halfLife = 0;
    private float _amount = 0;

    [Flags]
    private enum DirtyFlags
    {
        None = 0,
        Texture = 1 << 1,
        Center = 1 << 2,
        HalfLife = 1 << 3,
        Amount = 1 << 4,
        All = -1
    }

    private DirtyFlags _dirtyFlags = DirtyFlags.All;

    #endregion

    #region Properties

    public Texture? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            _dirtyFlags |= DirtyFlags.Texture;
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

    public float HalfLife
    {
        get => _halfLife;
        set
        {
            _halfLife = value;
            _dirtyFlags |= DirtyFlags.HalfLife;
        }
    }

    public float Amount
    {
        get => _amount;
        set
        {
            _amount = value;
            _dirtyFlags |= DirtyFlags.Amount;
        }
    }

    #endregion

    public ExposureEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, EffectResource.ExposureEffect.Bytecode)
    {
        _textureParam = Parameters["tex_sampler+tex"];
        _centerParam = Parameters["center"];
        _halfLifeParam = Parameters["half_life"];
        _amountParam = Parameters["amount"];
    }

    protected override void OnApply()
    {
        if ((_dirtyFlags & DirtyFlags.Texture) != DirtyFlags.None)
            _textureParam.SetValue(_texture);

        if ((_dirtyFlags & DirtyFlags.Center) != DirtyFlags.None)
            _centerParam.SetValue(_center);

        if ((_dirtyFlags & DirtyFlags.HalfLife) != DirtyFlags.None)
            _halfLifeParam.SetValue(_halfLife);

        if ((_dirtyFlags & DirtyFlags.Amount) != DirtyFlags.None)
            _amountParam.SetValue(_amount);

        _dirtyFlags = DirtyFlags.None;
    }
}
