using System.Reflection;
using System.Reflection.Emit;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;

namespace OpenSolarMax.Game.Utils;

using static InitializerHelper;

/// <summary>
/// 组件原地初始化帮助类。
/// 因为C#没有提供原地构造的语法，故使用<see cref="ILGenerator"/>生成实现原地构造的IL代码
/// </summary>
internal static class InitializerHelper
{
    public delegate void ComponentInitializer(in Entity entity);

    private static readonly Dictionary<Type, ComponentInitializer> _initializersCache = [];

    private static readonly MethodInfo _componentGetter = typeof(EntityExtensions)
        .GetMethod("Get", 1, BindingFlags.Static | BindingFlags.Public, null, [typeof(Entity).MakeByRefType()], null)!;

    private static ComponentInitializer? BuildDefaultInitializer(Type type)
    {
        var defaultConstructor = type.GetConstructor([]);
        if (defaultConstructor == null) //如果该组件没有默认构造函数，则返回空
            return null;

        var dynMethod = new DynamicMethod("Initialize", null, [typeof(Entity).MakeByRefType()]);
        var ilGenerator = dynMethod.GetILGenerator();

        ilGenerator.DeclareLocal(type.MakeByRefType());

        ilGenerator.Emit(OpCodes.Ldarg, 0);
        ilGenerator.Emit(OpCodes.Call, _componentGetter.MakeGenericMethod(type));
        ilGenerator.Emit(OpCodes.Stloc, 0);

        ilGenerator.Emit(OpCodes.Ldloc, 0);
        ilGenerator.Emit(OpCodes.Call, defaultConstructor);

        ilGenerator.Emit(OpCodes.Ret);

        return dynMethod.CreateDelegate<ComponentInitializer>();
    }

    public static ComponentInitializer? GetDefaultInitializer(Type type)
    {
        if (_initializersCache.TryGetValue(type, out var initializer))
            return initializer;

        initializer = BuildDefaultInitializer(type);
        if (initializer == null)
            return null;

        _initializersCache.Add(type, initializer);
        return initializer;
    }
}

/// <summary>
/// 原型定义。
/// 之所以不使用Arch推荐的<see cref="ComponentType[]"/>作为原型是因为其根据原型创建实体时不会对组件进行初始构造。
/// 使用该原型以及其扩展方法<see cref="ArchetypeExtensions.Construct(World, in Archetype)"/>可以在新建实体时对其组件进行初始化
/// </summary>
/// <param name="types">该原型拥有的所有组件类型</param>
public readonly struct Archetype(params Type[] types)
{
    private readonly Type[] _rawTypes = types;

    public readonly ComponentType[] ComponentTypes
        = (from type in types select (ComponentType)type).ToArray();

    internal readonly ComponentInitializer?[] Initializers
        = (from type in types select GetDefaultInitializer(type)).ToArray();

    /// <summary>
    /// 对两个原型求并集，生成一个新的拥有二者所有组件类型的新原型对象
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Archetype operator +(in Archetype left, in Archetype right)
        => new((left._rawTypes ?? []).Union(right._rawTypes ?? []).ToArray());
}

public static class ArchetypeExtensions
{
    /// <summary>
    /// 根据指定原型，构造一个新实体，并对其中可初始化的组件进行原地初始化
    /// </summary>
    /// <param name="world"></param>
    /// <param name="archetype"></param>
    /// <returns>新生成并初始化后的实体</returns>
    public static Entity Construct(this World world, in Archetype archetype)
    {
        var entity = world.Create(archetype.ComponentTypes);

        foreach (var initializer in archetype.Initializers)
            initializer?.Invoke(in entity);

        return entity;
    }
}
