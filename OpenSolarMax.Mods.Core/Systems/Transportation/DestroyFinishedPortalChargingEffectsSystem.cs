// using Arch.Buffer;
// using Arch.Core;
// using Arch.Core.Extensions;
// using Arch.System;
// using Arch.System.SourceGenerator;
// using Microsoft.Xna.Framework;
// using Nine.Assets;
// using OpenSolarMax.Game.ECS;
// using OpenSolarMax.Mods.Core.Components;
// 
// namespace OpenSolarMax.Mods.Core.Systems.Transportation;
// 
// [StructuralChangeSystem]
// [ExecuteBefore(typeof(ManageDependenceSystem))]
// public sealed partial class DestroyFinishedPortalChargingEffectsSystem(World world)
//     : BaseSystem<World, GameTime>(world), ISystem
// {
//     private static bool AnimationDone(in Animation animation)
//     {
//         if (animation.Clip is null) return true;
// 
//         return (animation.TimeElapsed + animation.TimeOffset).TotalSeconds > animation.Clip.Length;
//     }
// 
//     private readonly CommandBuffer _commandBuffer = new();
// 
//     [Query]
//     [All<PortalChargingEffectAssignment>]
//     private static void ExpireEffects([Data] CommandBuffer commands,
//                                       Entity entity, in PortalChargingEffectAssignment assignment)
//     {
//         if (AnimationDone(in assignment.BackFlare.Get<Animation>())
//             && assignment.SurroundFlares.All(r => AnimationDone(in r.Get<Animation>())))
//             commands.Destroy(entity);
//     }
// 
//     public override void Update(in GameTime d)
//     {
//         ExpireEffectsQuery(World, _commandBuffer);
//         _commandBuffer.Playback(World);
//     }
// }
