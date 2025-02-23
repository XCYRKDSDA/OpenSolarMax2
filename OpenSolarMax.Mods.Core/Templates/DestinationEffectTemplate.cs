using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations.Parametric;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class DestinationEffectTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required EntityReference Portal { get; set; }

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
        typeof(DestinationEffectAssignment)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        var backFlare = world.Make(new DestinationBackFlareTemplate(assets)
        {
            Effect = entity.Reference(),
            Radius = PortalRadius * 2f, Color = Color
        });

        var surroundFlares = new List<EntityReference>();
        for (int i = 0; i < 3; i++)
        {
            surroundFlares.Add(world.Make(new DestinationSurroundFlareTemplate(assets)
            {
                Effect = entity.Reference(),
                Radius = PortalRadius * 2f, Color = Color,
                Angle = i * MathF.PI * 2 / 3
            }).Reference());
        }

        entity.Set(new DestinationEffectAssignment(surroundFlares.ToArray(), backFlare.Reference()));

        _ = world.Make(new DependenceTemplate() { Dependent = entity.Reference(), Dependency = Portal });
        var tf = world.Make(new RelativeTransformTemplate()
        {
            Parent = Portal, Child = entity.Reference(),
            Translation = Vector3.Zero with { Z = 500 } // 保证位于前边
        });
    }
}
