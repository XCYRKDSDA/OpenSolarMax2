using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class DestinationEffectTemplate(IAssetsManager assets) : ITemplate
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
        typeof(DestinationEffectAssignment)
    );

    public Signature Signature => _signature;

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = world.Make(commandBuffer, new DestinationBackFlareTemplate(assets)
        {
            Effect = entity,
            Radius = PortalRadius * 2f,
            Color = Color
        });

        var surroundFlares = new List<Entity>();
        for (int i = 0; i < 3; i++)
        {
            surroundFlares.Add(world.Make(commandBuffer, new DestinationSurroundFlareTemplate(assets)
            {
                Effect = entity,
                Radius = PortalRadius * 2f,
                Color = Color,
                Angle = i * MathF.PI * 2 / 3
            }));
        }

        // TODO：检查 Entity 引用情况
        commandBuffer.Set(in entity, new DestinationEffectAssignment(surroundFlares.ToArray(), backFlare));

        world.Make(commandBuffer, new DependenceTemplate { Dependent = entity, Dependency = Portal });
        world.Make(commandBuffer, new RelativeTransformTemplate
        {
            Parent = Portal,
            Child = entity,
            Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
        });
    }
}
