using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(IndexShootSystem))]
[ExecuteAfter(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class AimLaserBeamSystem(World world) : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<Shoot.AsBeam, TreeRelationship<RelativeTransform>.AsChild, Sprite>]
    private static void Aim(in Shoot.AsBeam asBeam, in TreeRelationship<RelativeTransform>.AsChild asChild,
                            ref Sprite sprite)
    {
        if (asBeam.Relationship is null)
            return;

        Debug.Assert(asChild.Relationship is not null);

        ref readonly var targetPose = ref asBeam.Relationship.Value.Copy.Target.Entity.Get<AbsoluteTransform>();
        ref readonly var parentPose = ref asChild.Relationship.Value.Copy.Parent.Entity.Get<AbsoluteTransform>();

        var vector = targetPose.Translation - parentPose.Translation;
        var unitX = Vector3.Normalize(vector);
        var unitY = Vector3.Normalize(new(-vector.Y, vector.X, 0));
        var unitZ = Vector3.Cross(unitX, unitY);
        var rotation = new Matrix { Right = unitX, Up = unitY, Backward = unitZ };

        ref var relativeTransform = ref asChild.Relationship.Value.Ref.Entity.Get<RelativeTransform>();
        relativeTransform.Translation = Vector3.Zero;
        relativeTransform.Rotation = Quaternion.CreateFromRotationMatrix(rotation);

        sprite.Size.X = vector.Length();
    }
}
