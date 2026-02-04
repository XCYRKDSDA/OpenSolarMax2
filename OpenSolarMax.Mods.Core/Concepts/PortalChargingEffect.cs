using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PortalChargingEffect = "PortalChargingEffect";
}

[Define(ConceptNames.PortalChargingEffect)]
public abstract class PortalChargingEffectDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        //
        typeof(SoundEffect),
        typeof(PortalChargingEffectAssignment)
    );
}

[Describe(ConceptNames.PortalChargingEffect)]
public class PortalChargingEffectDescription : IDescription
{
    public required Entity Portal { get; set; }

    public required float PortalRadius { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.PortalChargingEffect)]
public class PortalChargingEffectApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<PortalChargingEffectDescription>
{
    private FmodEventDescription _warpChargingSoundEffect =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/WarpCharging");

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalChargingEffectDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = factory.Make(world, commandBuffer, ConceptNames.PortalChargingBackFlare,
                                     new PortalChargingBackFlareDescription
                                     {
                                         Effect = entity,
                                         Radius = desc.PortalRadius * 3f,
                                         Color = desc.Color
                                     });

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
                surroundFlares.Add(factory.Make(world, commandBuffer, ConceptNames.PortalChargingSurroundFlare,
                                                new PortalChargingSurroundFlareDescription
                                                {
                                                    Effect = entity,
                                                    Radius = desc.PortalRadius * 3f,
                                                    Color = desc.Color,
                                                    MaxSize = maxSize,
                                                    Ratio = rate,
                                                    Angle = angle,
                                                    Delay = delay
                                                }));

                delay += delayStep;
                angle += MathF.PI * 2 / 3;
            }
            rate *= 1.1f;
            delayStep *= 0.9f;
            maxSize *= 0.8f;
        }

        commandBuffer.Set(in entity, new PortalChargingEffectAssignment(surroundFlares.ToArray(), backFlare));

        factory.Make(world, commandBuffer, ConceptNames.Dependence,
                     new DependenceDescription { Dependent = entity, Dependency = desc.Portal });
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription
                     {
                         Parent = desc.Portal,
                         Child = entity,
                         Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
                     });

        _warpChargingSoundEffect.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
