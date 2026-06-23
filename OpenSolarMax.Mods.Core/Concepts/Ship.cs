using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Ship = "Ship";
}

[Define(ConceptNames.Ship)]
public abstract class ShipDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Sprite),
            typeof(SoundEffect),
            // 动画
            typeof(Animation),
            //
            typeof(InTeam.AsAffiliate),
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
    /// 舰船创建时所属的阵营。必须提供
    /// </summary>
    public required Entity Team { get; set; }
}

[Apply(ConceptNames.Ship)]
public class ShipApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<ShipDescription>
{
    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(
        Content.Textures.DefaultShip
    );

    private readonly AnimationClip<Entity> _shipBlinkingAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipBlinking.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _defaultTexture,
                Color = Color.White,
                Alpha = 1,
                Size = new(4, 4),
                Position = Vector2.Zero,
                Rotation = 0,
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
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

        // TODO 延迟化 设置所属星球
        var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(entity, desc.Planet);
        RevolutionUtils.RandomlySetShipOrbitAroundPlanet(transformRelationship, desc.Planet);

        // 设置所属阵营
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.InTeam,
            new InTeamDescription { Team = desc.Team, Affiliate = entity }
        );

        // 初始化飞行状态
        commandBuffer.Set(in entity, new JumpingStatus { State = JumpingState.Idle });

        // 初始化传送状态
        commandBuffer.Set(in entity, new WarpingStatus { State = WarpingState.Idle });

        // 初始化死亡状态
        commandBuffer.Set(in entity, new ShipDeathState { State = DeathState.Alive });
    }
}
