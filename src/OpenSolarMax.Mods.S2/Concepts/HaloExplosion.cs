using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string HaloExplosion = "HaloExplosion";
}

[Define(ConceptNames.HaloExplosion)]
public abstract class HaloExplosionDefinition : IDefinition
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

[Describe(ConceptNames.HaloExplosion)]
public class HaloExplosionDescription : IDescription
{
    public Entity Team { get; set; } = Entity.Null;

    public required Vector3 Position { get; set; }

    public required float PlanetRadius { get; set; }
}

[Apply(ConceptNames.HaloExplosion)]
public class HaloExplosionApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<HaloExplosionDescription>
{
    private readonly TextureRegion _haloTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Halo"
    );

    private readonly AnimationClip<Entity> _explosionAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.HaloExplosion_json
    );

    private readonly SafeFmodEventDescription _colonizedSoundEvent =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/PlanetColonized");

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, HaloExplosionDescription desc)
    {
        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new AbsoluteTransformOptions
                {
                    Translation = desc.Position with { Z = 1000 },
                },
                Texture = _haloTexture,
                Size = new(desc.PlanetRadius * 2),
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _explosionAnimation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        // 设置音效
        _colonizedSoundEvent.Native.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
