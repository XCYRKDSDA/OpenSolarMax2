using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Templates;

public class LaserBeamTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Color Color { get; set; }

    public required Entity Planet { get; set; }

    public required Vector3 TargetPosition { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 效果
        typeof(Sprite),
        typeof(SoundEffect),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationAndSoundEffectCompleted)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _beamTexture = assets.Load<TextureRegion>("Textures/TurretAtlas.json:Beam");

    private readonly AnimationClip<Entity> _beamAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/LaserBeam.json");

    private readonly FmodEventDescription _laserSoundEffect =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/LaserShoot");

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        ref readonly var turretPose = ref Planet.Get<AbsoluteTransform>();
        var vector = TargetPosition - turretPose.Translation;
        var unitX = Vector3.Normalize(vector);
        var unitY = Vector3.Normalize(new(-vector.Y, vector.X, 0));
        var unitZ = Vector3.Cross(unitX, unitY);
        var rotation = new Matrix { Right = unitX, Up = unitY, Backward = unitZ };
        world.Make(commandBuffer, new RelativeTransformTemplate
        {
            Parent = Planet,
            Child = entity,
            Translation = Vector3.Zero,
            Rotation = Quaternion.CreateFromRotationMatrix(rotation)
        });

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _beamTexture,
            Color = Color,
            Alpha = 1,
            Size = _beamTexture.LogicalSize with { X = vector.Length() },
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive,
            Billboard = false
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _beamAnimation,
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.Zero
        });

        // 设置音效
        _laserSoundEffect.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
