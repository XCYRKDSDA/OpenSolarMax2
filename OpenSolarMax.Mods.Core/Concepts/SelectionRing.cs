using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    /// <summary>
    /// 选择圈概念名称。用于在运行时创建选择圈实体。
    /// </summary>
    public const string SelectionRing = "SelectionRing";
}

/// <summary>
/// 选择圈实体定义。选择圈是 View 和 Planet 的交叉实体，用于可视化选择状态。
/// </summary>
[Define(ConceptNames.SelectionRing)]
public abstract class SelectionRingDefinition : IDefinition
{
    /// <summary>
    /// 选择圈实体的组件签名。
    /// </summary>
    public static Signature Signature { get; } = Signatures.SelectionRing;
}

/// <summary>
/// 选择圈实体描述。包含关联的 Planet 和 View 实体引用。
/// </summary>
[Describe(ConceptNames.SelectionRing)]
public class SelectionRingDescription : IDescription
{
    /// <summary>
    /// 关联的星球实体。
    /// </summary>
    public required Entity Planet { get; set; }

    /// <summary>
    /// 关联的视图实体。
    /// </summary>
    public required Entity View { get; set; }
}

/// <summary>
/// 选择圈实体应用器。创建选择圈实体并建立两个关系（Planet-SelectionRing 和 View-SelectionRing）。
/// </summary>
[Apply(ConceptNames.SelectionRing)]
public class SelectionRingApplier(IConceptFactory factory) : IApplier<SelectionRingDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, SelectionRingDescription desc)
    {
        // 设置选择圈的初始视觉状态（选中状态，Alpha=1）
        commandBuffer.Set(in entity, new SelectionRingVisual() { Alpha = 1, Scale = 1 });

        // 创建 Planet-SelectionRing 关系实体
        var planetRingDesc = new PlanetSelectionRingDescription()
        {
            Planet = desc.Planet,
            Ring = entity,
        };
        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.PlanetSelectionRing,
            planetRingDesc
        );

        // 创建 View-SelectionRing 关系实体
        var viewRingDesc = new ViewSelectionRingDescription() { View = desc.View, Ring = entity };
        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.ViewSelectionRing,
            viewRingDesc
        );
    }
}

// 行星-选择圈关系概念
public static partial class ConceptNames
{
    public const string PlanetSelectionRing = "PlanetSelectionRing";
}

[Define(ConceptNames.PlanetSelectionRing)]
public abstract class PlanetSelectionRingDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(PlanetSelectionRing));
}

[Describe(ConceptNames.PlanetSelectionRing)]
public class PlanetSelectionRingDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Ring { get; set; }
}

[Apply(ConceptNames.PlanetSelectionRing)]
public class PlanetSelectionRingApplier : IApplier<PlanetSelectionRingDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        PlanetSelectionRingDescription desc
    )
    {
        commandBuffer.Set(in entity, new PlanetSelectionRing(desc.Planet, desc.Ring));
    }
}

// 视图-选择圈关系概念
public static partial class ConceptNames
{
    public const string ViewSelectionRing = "ViewSelectionRing";
}

[Define(ConceptNames.ViewSelectionRing)]
public abstract class ViewSelectionRingDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(ViewSelectionRing));
}

[Describe(ConceptNames.ViewSelectionRing)]
public class ViewSelectionRingDescription : IDescription
{
    public required Entity View { get; set; }
    public required Entity Ring { get; set; }
}

[Apply(ConceptNames.ViewSelectionRing)]
public class ViewSelectionRingApplier : IApplier<ViewSelectionRingDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, ViewSelectionRingDescription desc)
    {
        commandBuffer.Set(in entity, new ViewSelectionRing(desc.View, desc.Ring));
    }
}
