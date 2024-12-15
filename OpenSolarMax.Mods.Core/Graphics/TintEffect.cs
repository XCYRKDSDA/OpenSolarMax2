using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Mods.Core.Graphics;

public class TintEffect : Effect, IEffectMatrices
{
    #region Effect Parameters

    private readonly EffectParameter _toNdcParam;

    private readonly EffectParameter _textureParam;

    #endregion

    #region Fields

    private Matrix _world = Matrix.Identity;
    private Matrix _view = Matrix.Identity;
    private Matrix _proj = Matrix.Identity;

    private Texture? _texture = null;

    [Flags]
    private enum DirtyFlags
    {
        None = 0,
        WorldViewProj = 1 << 1,
        Texture = 1 << 2,
        All = -1
    }

    private TintEffect.DirtyFlags _dirtyFlags = TintEffect.DirtyFlags.All;

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

    public Texture? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            _dirtyFlags |= DirtyFlags.Texture;
        }
    }

    #endregion

    public TintEffect(GraphicsDevice graphicsDevice, IAssetsManager assets)
        : base(graphicsDevice, assets.Load<byte[]>("Effects/Tint.mgfxo"))
    {
        _toNdcParam = Parameters["to_ndc"];
        _textureParam = Parameters["tex_sampler+tex"];
    }

    protected override void OnApply()
    {
        if ((_dirtyFlags & DirtyFlags.WorldViewProj) != DirtyFlags.None)
            _toNdcParam.SetValue(_world * _view * _proj);

        if ((_dirtyFlags & DirtyFlags.Texture) != DirtyFlags.None)
            _textureParam.SetValue(_texture);
    }
}
