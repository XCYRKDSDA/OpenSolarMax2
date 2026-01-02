using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Templates;

public class PortalChargingEffectTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Portal { get; set; }

    public required float PortalRadius { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Signature _signature = new(
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

    public Signature Signature => _signature;

    private FmodEventDescription _warpChargingSoundEffect =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/WarpCharging");

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = world.Make(commandBuffer, new PortalChargingBackFlareTemplate(assets)
        {
            Effect = entity,
            Radius = PortalRadius * 3f,
            Color = Color
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
                surroundFlares.Add(world.Make(commandBuffer, new PortalChargingSurroundFlareTemplate(assets)
                {
                    Effect = entity,
                    Radius = PortalRadius * 3f,
                    Color = Color,
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

        world.Make(commandBuffer, new DependenceTemplate { Dependent = entity, Dependency = Portal });
        world.Make(commandBuffer, new RelativeTransformTemplate
        {
            Parent = Portal,
            Child = entity,
            Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
        });

        _warpChargingSoundEffect.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}
