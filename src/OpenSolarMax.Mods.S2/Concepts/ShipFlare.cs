using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ShipFlare = "ShipFlare";
}

[Define(ConceptNames.ShipFlare)]
public abstract class ShipFlareDefinition : IDefinition
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

[Describe(ConceptNames.ShipFlare)]
public class ShipFlareDescription : IDescription
{
    public required Vector3 Position { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.ShipFlare)]
public class ShipFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipFlareDescription>
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":ShipFlare"
    );

    private readonly AnimationClip<Entity> _flareAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.ShipFlare_json
    );

    private readonly SafeFmodEventDescription _destroyedSoundEvent =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/ShipDestroyed");

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipFlareDescription desc)
    {
        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new AbsoluteTransformOptions { Translation = desc.Position },
                Texture = _flareTexture,
                Size = new(4, 4),
                Scale = Vector2.Zero,
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _flareAnimation,
                TimeOffset = TimeSpan.Zero,
                TimeElapsed = TimeSpan.Zero,
            }
        );

        // 设置音效
        _destroyedSoundEvent.Native.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
