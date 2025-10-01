using System.Reflection;
using System.Reflection.Emit;
using Arch.Buffer;
using Arch.Core;

namespace OpenSolarMax.Game.Utils;

using static InitializerHelper;

/// <summary>
/// 组件原地初始化帮助类。
/// 因为C#没有提供原地构造的语法，故使用<see cref="ILGenerator"/>生成实现原地构造的IL代码
/// </summary>
internal static class InitializerHelper
{
    #region In-place initializer

    public delegate void ComponentInitializer(in Entity entity);

    private static readonly Dictionary<Type, ComponentInitializer> _initializersCache = [];

    private static readonly MethodInfo _componentGetter = typeof(Arch.Core.Extensions.EntityExtensions)
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

    #endregion

    #region CommandBuffer constructor

    public delegate void ComponentConstructor(CommandBuffer commandBuffer, in Entity entity);

    private static readonly Dictionary<Type, ComponentConstructor> _constructorsCache = [];

    private static void ConstructComponent<T>(CommandBuffer commandBuffer, in Entity entity) where T : new()
    {
        commandBuffer.Add(in entity, new T());
    }

    private static readonly MethodInfo _componentConstructor = typeof(InitializerHelper).GetMethod(
        "ConstructComponent", BindingFlags.Static | BindingFlags.NonPublic)!;

    public static ComponentConstructor? GetDefaultConstructor(Type type)
    {
        if (_constructorsCache.TryGetValue(type, out var constructor))
            return constructor;

        // 如果没有默认构造函数，则返回空
        if (type.GetConstructor([]) is null)
            return null;

        constructor = _componentConstructor.MakeGenericMethod([type]).CreateDelegate<ComponentConstructor>();
        _constructorsCache.Add(type, constructor);

        return constructor;
    }

    #endregion
}

public static class SignatureExtensions
{
    /// <summary>
    /// 根据指定签名，构造一个新实体，并对其中可初始化的组件进行原地初始化
    /// </summary>
    /// <param name="world"></param>
    /// <param name="signature"></param>
    /// <returns>新生成并初始化后的实体</returns>
    public static Entity Construct(this World world, in Signature signature)
    {
        var entity = world.Create(signature);

        foreach (var component in signature.Components)
        {
            var initializer = GetDefaultInitializer(component.Type);
            initializer?.Invoke(in entity);
        }

        return entity;
    }

    public static Entity Construct(this World world, CommandBuffer commandBuffer, in Signature signature)
    {
        var entity = world.Create(); // 创建空实体用于占位

        foreach (var component in signature.Components)
            GetDefaultConstructor(component.Type)?.Invoke(commandBuffer, in entity);

        return entity;
    }
}
