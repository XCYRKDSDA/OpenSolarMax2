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
        new(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 效果
            typeof(Sprite),
            typeof(SoundEffect),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationAndSoundEffectCompleted),
            // 阵营
            typeof(InTeam.AsAffiliate)
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

    public void Apply(CommandBuffer commandBuffer, Entity entity, HaloExplosionDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        commandBuffer.Set(
            in entity,
            new AbsoluteTransform { Translation = desc.Position with { Z = 1000 } }
        );

        // 设置纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _haloTexture,
                Alpha = 1,
                Size = new(desc.PlanetRadius * 2),
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
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

        // 设置阵营
        if (desc.Team != Entity.Null)
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.InTeam,
                new InTeamDescription { Team = desc.Team, Affiliate = entity }
            );
    }
}
