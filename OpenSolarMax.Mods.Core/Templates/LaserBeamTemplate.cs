using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;
using FmodEventDescription = FMOD.Studio.EventDescription;
using Vector3 = System.Numerics.Vector3;

namespace OpenSolarMax.Mods.Core.Templates;

public class LaserBeamTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Color Color { get; set; }

    public required EntityReference Planet { get; set; }

    public required EntityReference Target { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 效果
        typeof(Sprite),
        typeof(SoundEffect),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationAndSoundEffectCompleted),
        //
        typeof(Shoot.AsBeam)
    );

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _beamTexture = assets.Load<TextureRegion>("Textures/TurretAtlas.json:Beam");

    private readonly AnimationClip<Entity> _beamAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/LaserBeam.json");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        // 只需要将原点放在星球位置，后续方向会自动计算
        world.Make(new RelativeTransformTemplate()
        {
            Parent = Planet,
            Child = entity.Reference(),
            Translation = Vector3.Zero
        });

        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _beamTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = _beamTexture.LogicalSize; // 后续由专用系统调整
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Billboard = false;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _beamAnimation;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;

        // 创建关系
        world.Make(new ShootTemplate()
        {
            Beam = entity.Reference(),
            Target = Target
        });
    }
}
