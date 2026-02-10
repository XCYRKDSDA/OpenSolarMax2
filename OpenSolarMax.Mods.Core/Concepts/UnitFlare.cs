using Arch.Buffer;
using Arch.Core;
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
    public const string UnitFlare = "UnitFlare";
}

[Define(ConceptNames.UnitFlare)]
public abstract class UnitFlareDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 位姿변환
        typeof(AbsoluteTransform),
        // 효과
        typeof(Sprite),
        typeof(SoundEffect),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationAndSoundEffectCompleted)
    );
}

[Describe(ConceptNames.UnitFlare)]
public class UnitFlareDescription : IDescription
{
    public required Vector3 Position { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.UnitFlare)]
public class UnitFlareApplier(IAssetsManager assets) : IApplier<UnitFlareDescription>
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipFlare");

    private readonly AnimationClip<Entity> _flareAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/UnitFlare.json");

    private FmodEventDescription _destroyedSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/UnitDestroyed");

    public void Apply(CommandBuffer commandBuffer, Entity entity, UnitFlareDescription desc)
    {
        // 设置位置
        commandBuffer.Set(in entity, new AbsoluteTransform
        {
            Translation = desc.Position
        });

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _flareTexture,
            Color = desc.Color,
            Alpha = 1,
            Size = _flareTexture.LogicalSize,
            Scale = Vector2.One * 0.001f,
            Blend = SpriteBlend.Additive
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _flareAnimation,
            TimeOffset = TimeSpan.Zero,
            TimeElapsed = TimeSpan.Zero
        });

        // 设置音效
        _destroyedSoundEvent.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
