using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class DependenceUtils
{
    /// <summary>
    /// 断言二者有能力构建依赖关系
    /// </summary>
    [Conditional("DEBUG")]
    public static void DebugAssert_AbleToBuildRelationship(Entity dependent, Entity dependency)
    {
        Debug.Assert(dependent.WorldId == dependency.WorldId);

        Debug.Assert(dependent.Has<Dependence.AsDependent>());
        Debug.Assert(dependency.Has<Dependence.AsDependency>());
    }

    public static void SetDependence(Entity dependent, Entity dependency)
    {
        DebugAssert_AbleToBuildRelationship(dependent, dependency);

        var world = World.Worlds[dependent.WorldId];

        var relationship = world.Create<Dependence>(new(dependent, dependency));
        dependent.Get<Dependence.AsDependent>().Relationships.Add(dependency, relationship);
        dependency.Get<Dependence.AsDependency>().Relationships.Add(dependent, relationship);
    }

    public static void DependOn(this Entity dependent, Entity dependency)
        => SetDependence(dependent, dependency);

    /// <summary>
    /// 断言二者确实处在一个依赖关系中
    /// </summary>
    [Conditional("DEBUG")]
    public static void DebugAssert_InRelationship(Entity dependent, Entity dependency)
    {
        Debug.Assert(dependent.WorldId == dependency.WorldId);

        Debug.Assert(dependent.Has<Dependence.AsDependent>());
        Debug.Assert(dependency.Has<Dependence.AsDependency>());

        ref readonly var asDependent = ref dependent.Get<Dependence.AsDependent>();
        ref readonly var asDependency = ref dependency.Get<Dependence.AsDependency>();

        Debug.Assert(asDependent.Relationships.ContainsKey(dependency));
        Debug.Assert(asDependency.Relationships.ContainsKey(dependent));
        Debug.Assert(asDependent.Relationships[dependency] == asDependency.Relationships[dependent]);
    }

    public static void RemoveDependence(Entity dependent, Entity dependency)
    {
        DebugAssert_InRelationship(dependent, dependency);

        ref readonly var asDependent = ref dependent.Get<Dependence.AsDependent>();
        ref readonly var asDependency = ref dependency.Get<Dependence.AsDependency>();

        var world = World.Worlds[dependent.WorldId];
        world.Destroy(asDependent.Relationships[dependency]);
        asDependent.Relationships.Remove(dependency);
        asDependency.Relationships.Remove(dependent);
    }

    public static void NoLongerDependOn(this Entity dependent, Entity dependency)
        => RemoveDependence(dependent, dependency);
}
