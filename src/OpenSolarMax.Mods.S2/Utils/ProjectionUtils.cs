using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Utils;

internal static class ProjectionUtils
{
    /// <summary>
    /// 判断屏幕实体是否通过投影关系属于指定视图
    /// </summary>
    public static bool ProjectsTo(this Entity entity, Entity view)
    {
        if (!entity.TryGet<WorldToScreenProjection.AsTarget>(out var asTarget))
            return false;

        return asTarget.Relationship is { } relationship && relationship.Copy.View == view;
    }
}
