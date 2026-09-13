using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;

// 接管 Arch.Core.Entity 悬停预览名称
[assembly: DebuggerDisplay(
    "{OpenSolarMax.Game.Modding.Concept.ConceptDebugger.GetName(this),nq}",
    Target = typeof(Entity)
)]

namespace OpenSolarMax.Game.Modding.Concept;

/// <summary>
/// 调试用：标记实体所属概念的名字组件
/// </summary>
public struct ConceptTag
{
    public string Name;
}

/// <summary>
/// 调试用：概念名查询，供 DebuggerDisplay 表达式求值
/// </summary>
public static class ConceptDebugger
{
    public static string GetName(Entity entity)
    {
        var conceptName = entity.TryGet<ConceptTag>(out var tag) ? tag.Name : "Unknown";

        return $"{conceptName} #{entity.Id} W{entity.WorldId}";
    }
}
