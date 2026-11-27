using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Ecs.Concept;
using OpenSolarMax.Ecs.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Concepts;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string Cursor = "Cursor";
}

[Define(ConceptNames.Cursor)]
public abstract class CursorDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(AbsoluteTransform),
            // 关系相关
            typeof(ViewCursor.AsCursor), // 视图与光标的关系索引
            typeof(ManeuveringLineTarget.AsTarget), // 作为拖拽线目标的索引
            typeof(ReachabilityRelation.AsDestination), // 作为可达性关系目的端的索引
            typeof(Dependence.AsDependent)
        );
}

[Describe(ConceptNames.Cursor)]
public class CursorDescription : IDescription
{
    /// <summary>
    /// 光标所属的视图
    /// </summary>
    public required Entity View { get; set; }
}

[Apply(ConceptNames.Cursor)]
public class CursorApplier(IConceptFactory factory) : IApplier<CursorDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, CursorDescription desc)
    {
        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            new ViewCursorDescription { View = desc.View, Cursor = entity }
        );

        // 光标依赖视图，视图销毁时光标一并销毁
        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            new DependenceDescription { Dependent = entity, Dependency = desc.View }
        );
    }
}
