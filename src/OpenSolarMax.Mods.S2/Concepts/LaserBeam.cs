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
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Utils;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string LaserBeam = "LaserBeam";
}

[Define(ConceptNames.LaserBeam)]
public abstract class LaserBeamDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 效果
            typeof(SoundEffect),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationAndSoundEffectCompleted)
        );
}

[Describe(ConceptNames.LaserBeam)]
public class LaserBeamDescription : IDescription
{
    public Entity Team { get; set; } = Entity.Null;

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
        Content.Textures.SolarMax2_Atlas_json + ":Quad_16x4Glow"
    );

    private readonly AnimationClip<Entity> _beamAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.LaserBeam_json
    );

    private readonly SafeFmodEventDescription _laserSoundEffect =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/LaserShoot");

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, LaserBeamDescription desc)
    {
        // 计算射向
        ref readonly var towerPose = ref desc.Planet.Get<AbsoluteTransform>();
        var vector = desc.TargetPosition - towerPose.Translation;

        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new RelativeTransformOptions
                {
                    Parent = desc.Planet,
                    Rotation = TransformProjection.UprightAim(vector),
                },
                Texture = _beamTexture,
                Size = new Vector2(vector.Length(), _beamWidth),
                Blend = SpriteBlend.Additive,
                Billboard = false,
                Team = desc.Team,
                VisualStyle = VisualStyle.Effect,
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
