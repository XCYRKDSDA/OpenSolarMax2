using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Templates;

public class PortalChargingEffectTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Portal { get; set; }

    public required float PortalRadius { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
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

    public Archetype Archetype => _archetype;

    private FmodEventDescription _warpChargingSoundEffect =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/WarpCharging");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = world.Make(new PortalChargingBackFlareTemplate(assets)
        {
            Effect = entity,
            Radius = PortalRadius * 3f, Color = Color
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
                surroundFlares.Add(world.Make(new PortalChargingSurroundFlareTemplate(assets)
                {
                    Effect = entity,
                    Radius = PortalRadius * 3f, Color = Color,
                    MaxSize = maxSize, Ratio = rate, Angle = angle, Delay = delay
                }));

                delay += delayStep;
                angle += MathF.PI * 2 / 3;
            }
            rate *= 1.1f;
            delayStep *= 0.9f;
            maxSize *= 0.8f;
        }

        entity.Set(new PortalChargingEffectAssignment(surroundFlares.ToArray(), backFlare));

        _ = world.Make(new DependenceTemplate() { Dependent = entity, Dependency = Portal });
        _ = world.Make(new RelativeTransformTemplate()
        {
            Parent = Portal, Child = entity,
            Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
        });

        ref var soundEffect = ref entity.Get<SoundEffect>();
        _warpChargingSoundEffect.createInstance(out soundEffect.EventInstance);
        soundEffect.EventInstance.start();
    }
}
