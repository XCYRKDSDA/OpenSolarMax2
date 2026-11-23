using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string Ship = "Ship";
}

[Define(ConceptNames.Ship)]
public abstract class ShipDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 效果
            typeof(SoundEffect),
            // 动画
            typeof(Animation),
            //
            typeof(TreeRelationship<Anchorage>.AsChild),
            typeof(TrailOf.AsShip),
            typeof(JumpingStatus),
            typeof(PopulationCost),
            typeof(WarpingStatus),
            typeof(ShipDeathState)
        );
}

[Describe(ConceptNames.Ship)]
public class ShipDescription : IDescription
{
    /// <summary>
    /// 舰船创建时所在的星球。必须提供
    /// </summary>
    public required Entity Planet { get; set; }

    /// <summary>
    /// 舰船创建时所在星球的同步轨道。必须提供
    /// </summary>
    public required PlanetGeostationaryOrbit PlanetOrbit { get; set; }

    /// <summary>
    /// 舰船创建时所属的阵营。必须提供
    /// </summary>
    public required Entity Team { get; set; }
}

[Apply(ConceptNames.Ship)]
public class ShipApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<ShipDescription>
{
    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Ship"
    );

    private readonly AnimationClip<Entity> _shipBlinkingAnimationClip = assets.Load<
        AnimationClip<Entity>
    >(Content.Animations.ShipBlinking_json);

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipDescription desc)
    {
        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Texture = _defaultTexture,
                Alpha = 1,
                Size = new(4, 4),
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 设置闪烁动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _shipBlinkingAnimationClip,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.FromSeconds(new Random().NextDouble()),
            }
        );

        // 占用一个人口
        commandBuffer.Set(in entity, new PopulationCost { Value = 1 });

        var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(
            commandBuffer,
            entity,
            desc.Planet
        );
        RevolutionUtils.RandomlySetShipOrbitAroundPlanet(
            commandBuffer,
            transformRelationship,
            desc.PlanetOrbit
        );

        // 初始化飞行状态
        commandBuffer.Set(in entity, new JumpingStatus { State = JumpingState.Idle });

        // 初始化传送状态
        commandBuffer.Set(in entity, new WarpingStatus { State = WarpingState.Idle });

        // 初始化死亡状态
        commandBuffer.Set(in entity, new ShipDeathState { State = DeathState.Alive });
    }
}
