using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string LaserBeam = "LaserBeam";
}

[Define(ConceptNames.LaserBeam)]
public abstract class LaserBeamDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature
        + new Signature(
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
public class LaserBeamApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:laser_beam")] IConfiguration configs
) : IApplier<LaserBeamDescription>
{
    private readonly float _beamWidth = configs.RequireValue<float>("width");

    private readonly TextureRegion _beamTexture = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:Quad_16x4Glow"
    );

    private readonly AnimationClip<Entity> _beamAnimation = assets.Load<AnimationClip<Entity>>(
        "Animations/LaserBeam.json"
    );

    private readonly SafeFmodEventDescription _laserSoundEffect =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/LaserShoot");

    public void Apply(CommandBuffer commandBuffer, Entity entity, LaserBeamDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        ref readonly var turretPose = ref desc.Planet.Get<AbsoluteTransform>();
        var vector = desc.TargetPosition - turretPose.Translation;
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription
            {
                Parent = desc.Planet,
                Child = entity,
                Translation = Vector3.Zero,
                Rotation = TransformProjection.UprightAim(vector),
            }
        );

        // 设置纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _beamTexture,
                Color = desc.Color,
                Alpha = 1,
                Size = new Vector2(vector.Length(), _beamWidth),
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
                Billboard = false,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _beamAnimation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        // 设置音效
        _laserSoundEffect.Native.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
