using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string LaserBeam = "LaserBeam";
}

[Define(ConceptNames.LaserBeam)]
public abstract class LaserBeamDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
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
}

[Describe(ConceptNames.LaserBeam)]
public class LaserBeamDescription : IDescription
{
    public required Color Color { get; set; }

    public required Entity Planet { get; set; }

    public required Vector3 TargetPosition { get; set; }
}

[Apply(ConceptNames.LaserBeam)]
public class LaserBeamApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<LaserBeamDescription>
{
    private readonly TextureRegion _beamTexture = assets.Load<TextureRegion>("Textures/TurretAtlas.json:Beam");

    private readonly AnimationClip<Entity> _beamAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/LaserBeam.json");

    private readonly FmodEventDescription _laserSoundEffect =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/LaserShoot");

    public void Apply(CommandBuffer commandBuffer, Entity entity, LaserBeamDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        ref readonly var turretPose = ref desc.Planet.Get<AbsoluteTransform>();
        var vector = desc.TargetPosition - turretPose.Translation;
        var unitX = Vector3.Normalize(vector);
        var unitY = Vector3.Normalize(new(-vector.Y, vector.X, 0));
        var unitZ = Vector3.Cross(unitX, unitY);
        var rotation = new Matrix { Right = unitX, Up = unitY, Backward = unitZ };
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription
                     {
                         Parent = desc.Planet,
                         Child = entity,
                         Translation = Vector3.Zero,
                         Rotation = Quaternion.CreateFromRotationMatrix(rotation)
                     });

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _beamTexture,
            Color = desc.Color,
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
