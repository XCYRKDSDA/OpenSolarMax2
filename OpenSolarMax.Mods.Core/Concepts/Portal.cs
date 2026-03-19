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
    public const string Portal = "Portal";
}

[Define(ConceptNames.Portal)]
public abstract class PortalDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature
        + new Signature(
            // 传送任务
            typeof(PortalChargingJobs)
        );
}

[Describe(ConceptNames.Portal)]
public class PortalDescription : IDescription
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
    public Entity Party { get; set; } = Entity.Null;
}

[Apply(ConceptNames.Portal)]
public class PortalApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:portal")] IConfiguration configs
) : IApplier<PortalDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");
    private readonly int _volume = configs.RequireValue<int>("volume");

    private readonly TextureRegion _portalShape = assets.Load<TextureRegion>(
        "/Textures/PortalAtlas.json:Shape"
    );

    private readonly TextureRegion _portalTexture = assets.Load<TextureRegion>(
        "/Textures/PortalAtlas.json:Portal"
    );

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyDescription()
            {
                Shape = _portalShape,
                Texture = _portalTexture,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Party = desc.Party,
                Volume = _volume,
            }
        );

        // 初始化传送任务
        commandBuffer.Set(in entity, new PortalChargingJobs());
    }
}
