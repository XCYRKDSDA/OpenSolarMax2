using System.Reflection;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DisableAttribute : Attribute
{ }

internal enum ReadReference
{
    LastFrame,
    NextFrame,
}

internal static partial class Moddings
{
    /// <summary>
    /// 从一个程序集中找到所有的系统类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>各种类型系统类型的集合</returns>
    public static SystemTypeCollection FindSystemTypes(Assembly assembly)
    {
        var systemTypes = new SystemTypeCollection();

        foreach (var type in assembly.GetExportedTypes())
        {
            // 排除抽象类、接口、泛型类
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                continue;

            // 筛选实现了ISystem或IStructuralChangeSystem的类型
            if (!type.GetInterfaces().Contains(typeof(ISystem)) &&
                !type.GetInterfaces().Contains(typeof(IStructuralChangeSystem)))
                continue;

            // 排除禁用的系统
            if (type.GetCustomAttribute<DisableAttribute>() is not null)
                continue;

            if (type.GetCustomAttribute<SimulateSystemAttribute>() is not null)
                systemTypes.SimulateSystemTypes.Add(type);

            else if (type.GetCustomAttribute<InputSystemAttribute>() is not null)
                systemTypes.InputSystemTypes.Add(type);

            else if (type.GetCustomAttribute<AiSystemAttribute>() is not null)
                systemTypes.AiSystemTypes.Add(type);

            else if (type.GetCustomAttribute<RenderSystemAttribute>() is not null)
                systemTypes.RenderSystemTypes.Add(type);
        }

        return systemTypes;
    }
}
