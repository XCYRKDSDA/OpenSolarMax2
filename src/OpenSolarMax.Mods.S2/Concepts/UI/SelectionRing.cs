using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    /// <summary>
    /// 选择圈概念名称。用于在运行时创建选择圈实体。
    /// </summary>
    public const string SelectionRing = "SelectionRing";
}

/// <summary>
/// 选择圈实体定义。选择圈是星球的可视化实体，用于表示选择状态。
/// </summary>
[Define(ConceptNames.SelectionRing)]
public abstract class SelectionRingDefinition : IDefinition
{
    /// <summary>
    /// 选择圈实体的组件签名。选择圈不需要位置组件（位置从关联的星球获取）。
    /// </summary>
    public static Signature Signature { get; } =
        new(
            typeof(Shape),
            typeof(ScreenTransform),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted),
            typeof(WorldToScreenProjection.AsTarget),
            typeof(PlanetSelectionRing.AsRing),
            typeof(PlanetCandidateRing.AsRing)
        );
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
/// 选择圈实体应用器。创建选择圈实体并建立与星球的 Planet-SelectionRing 关系。
/// </summary>
[Apply(ConceptNames.SelectionRing)]
public class SelectionRingApplier(
    IConceptFactory factory,
    [Section("applier:selection_ring")] IConfiguration configs
) : IApplier<SelectionRingDescription>
{
    private readonly Color _color = configs.RequireValue<Color>("color");

    public void Apply(CommandBuffer commandBuffer, Entity entity, SelectionRingDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new Shape
            {
                Geometry = new Shape.Ring(Vector2.Zero, 0, 0, 0, Shape.ThicknessAlignment.Centered),
                Fill = _color,
            }
        );
        commandBuffer.Set(in entity, new ScreenTransform { Z = (int)GraphicsLayer.Interface + 1 });

        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.WorldToScreenProjection,
            new WorldToScreenProjectionDescription
            {
                Source = desc.Planet,
                Target = entity,
                View = desc.View,
                ProjectRotation = false,
            }
        );

        // 创建 Planet-SelectionRing 关系实体
        var planetRingDesc = new PlanetSelectionRingDescription()
        {
            Planet = desc.Planet,
            Ring = entity,
            View = desc.View,
        };
        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.PlanetSelectionRing,
            planetRingDesc
        );
    }
}
