using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string WarpChargingEffect = "WarpChargingEffect";
}

[Define(ConceptNames.WarpChargingEffect)]
public abstract class WarpChargingEffectDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + new Signature(
            //
            typeof(SoundEffect),
            typeof(WarpChargingEffectAssignment)
        );
}

[Describe(ConceptNames.WarpChargingEffect)]
public class WarpChargingEffectDescription : IDescription
{
    public required Entity Warp { get; set; }

    public required float WarpRadius { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.WarpChargingEffect)]
public class WarpChargingEffectApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<WarpChargingEffectDescription>
{
    private readonly SafeFmodEventDescription _warpChargingSoundEffect =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/WarpCharging");

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        WarpChargingEffectDescription desc
    )
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = factory.Make(
            world,
            commandBuffer,
            ConceptNames.WarpChargingBackFlare,
            new WarpChargingBackFlareDescription
            {
                Effect = entity,
                Radius = desc.WarpRadius * 3f,
                Color = desc.Color,
            }
        );

        float rate = 2.6f;
        float maxSize = 1;
        float delay = 0;
        float delayStep = 0.12f;
        float angle = 0;

        var surroundFlares = new List<Entity>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                surroundFlares.Add(
                    factory.Make(
                        world,
                        commandBuffer,
                        ConceptNames.WarpChargingSurroundFlare,
                        new WarpChargingSurroundFlareDescription
                        {
                            Effect = entity,
                            Radius = desc.WarpRadius * 3f,
                            Color = desc.Color,
                            MaxSize = maxSize,
                            Ratio = rate,
                            Angle = angle,
                            Delay = delay,
                        }
                    )
                );

                delay += delayStep;
                angle += MathF.PI * 2 / 3;
            }
            rate *= 1.1f;
            delayStep *= 0.9f;
            maxSize *= 0.8f;
        }

        commandBuffer.Set(
            in entity,
            new WarpChargingEffectAssignment(surroundFlares.ToArray(), backFlare)
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Warp }
        );
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription
            {
                Parent = desc.Warp,
                Child = entity,
                Translation = Vector3.Zero with { Z = 500 }, // 保证位于前边
            }
        );

        _warpChargingSoundEffect.Native.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
