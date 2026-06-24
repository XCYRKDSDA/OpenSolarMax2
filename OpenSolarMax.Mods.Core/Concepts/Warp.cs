using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Warp = "Warp";
}

[Define(ConceptNames.Warp)]
public abstract class WarpDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature
        + new Signature(
            // 传送任务
            typeof(WarpChargingJobs)
        );
}

[Describe(ConceptNames.Warp)]
public class WarpDescription : IDescription
{
    /// <summary>
    /// 传送门的变换关系
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 传送门所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.Warp)]
public class WarpApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:warp")] IConfiguration configs
) : IApplier<WarpDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");
    private readonly int _volume = configs.RequireValue<int>("volume");

    private readonly TextureRegion _warpTexture = assets.Load<TextureRegion>(
        "/Textures/SolarMax2.Atlas.json:Warp"
    );

    private readonly TextureRegion _warpShape = assets.Load<TextureRegion>(
        "/Textures/SolarMax2.Atlas.json:WarpShape"
    );

    private readonly TextureRegion _warpGlow = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:WarpGlow"
    );

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, WarpDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyDescription()
            {
                Shape = _warpShape,
                Texture = _warpTexture,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
                Volume = _volume,
                GlowTexture = _warpGlow,
            }
        );

        // 初始化传送任务
        commandBuffer.Set(in entity, new WarpChargingJobs());
    }
}
