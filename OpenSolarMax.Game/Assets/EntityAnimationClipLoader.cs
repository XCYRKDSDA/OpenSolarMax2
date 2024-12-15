using System.Reflection;
using System.Reflection.Emit;
using Arch.Core;
using Arch.Core.Extensions;
using Nine.Animations;
using Nine.Assets;

namespace OpenSolarMax.Game.Assets;

internal class EntityAnimationClipLoader : AnimationClipLoaderBase<Entity>
{
    #region Property Builder

    public List<Type> ComponentTypes { get; set; } = [];

    private static Type GetType(string typeName, IEnumerable<Type> types)
    {
        foreach (var type in types.Reverse())
        {
            if (type.FullName == typeName
                || (!typeName.Contains('.') && type.FullName!.Split('.')[^1] == typeName))
                return type;
        }

        throw new KeyNotFoundException();
    }

    private static Type GetMemberType(Type componentType, string memberPath)
    {
        // 获取成员类型
        var memberType = componentType;
        foreach (var part in memberPath.Split('.'))
            memberType = memberType.GetField(part) is { } field ? field.FieldType :
                         memberType.GetProperty(part) is { } property ? property.PropertyType :
                         throw new KeyNotFoundException();
        return memberType;
    }

    private static readonly MethodInfo _componentGetter =
        typeof(EntityExtensions).GetMethod("Get", 1, [typeof(Entity).MakeByRefType()])!;

    private static Delegate CompileGetter(Type componentType, Type memberType, string memberPath)
    {
        var dynMethod = new DynamicMethod("GetMember", memberType, [typeof(Entity).MakeByRefType()]);
        var ilGenerator = dynMethod.GetILGenerator();
        var localVariablesCount = 0;

        ilGenerator.Emit(OpCodes.Ldarg, 0);
        ilGenerator.Emit(OpCodes.Call, _componentGetter.MakeGenericMethod(componentType));

        var typeSoFar = componentType;
        foreach (var part in memberPath.Split('.'))
        {
            if (typeSoFar.GetField(part) is { } nextField)
            {
                ilGenerator.Emit(OpCodes.Ldflda, nextField);
                typeSoFar = nextField.FieldType;
            }
            else if (typeSoFar.GetProperty(part) is { } nextProperty)
            {
                ilGenerator.DeclareLocal(nextProperty.PropertyType);
                localVariablesCount += 1;

                ilGenerator.Emit(OpCodes.Call, nextProperty.GetMethod!);
                ilGenerator.Emit(OpCodes.Stloc, localVariablesCount - 1);

                ilGenerator.Emit(OpCodes.Ldloca, localVariablesCount - 1);
            }
        }

        ilGenerator.Emit(OpCodes.Ldobj, memberType);
        ilGenerator.Emit(OpCodes.Ret);

        return dynMethod.CreateDelegate(typeof(Getter<>).MakeGenericType(typeof(Entity), memberType));
    }

    private static Delegate CompileSetter(Type componentType, Type memberType, string memberPath)
    {
        var dynMethod =
            new DynamicMethod("SetMember", null, [typeof(Entity).MakeByRefType(), memberType.MakeByRefType()]);
        var ilGenerator = dynMethod.GetILGenerator();
        var variablesStack = new Stack<(LocalBuilder, LocalBuilder, PropertyInfo)>(); // 属性缓存、属性属于的对象的引用、属性信息

        ilGenerator.Emit(OpCodes.Ldarg, 0);
        ilGenerator.Emit(OpCodes.Call, _componentGetter.MakeGenericMethod(componentType));

        var typeSoFar = componentType;
        foreach (var part in memberPath.Split('.'))
        {
            if (typeSoFar.GetField(part) is { } field)
            {
                ilGenerator.Emit(OpCodes.Ldflda, field);
                typeSoFar = field.FieldType;
            }
            else if (typeSoFar.GetProperty(part) is { } property)
            {
                // 缓存当前字段的引用
                var fieldRef = ilGenerator.DeclareLocal(typeSoFar.MakeByRefType());
                ilGenerator.Emit(OpCodes.Stloc, fieldRef);

                // 创建并记录属性缓存
                var cacheVar = ilGenerator.DeclareLocal(property.PropertyType);
                variablesStack.Push((cacheVar, fieldRef, property));

                // 读取属性
                ilGenerator.Emit(OpCodes.Ldloc, fieldRef);
                ilGenerator.Emit(OpCodes.Call, property.GetMethod!);
                ilGenerator.Emit(OpCodes.Stloc, cacheVar);

                // 将属性缓存的地址压入计算栈
                ilGenerator.Emit(OpCodes.Ldloca, cacheVar);
                typeSoFar = property.PropertyType;
            }
        }

        ilGenerator.Emit(OpCodes.Ldarg, 1);
        ilGenerator.Emit(OpCodes.Ldobj, memberType);
        ilGenerator.Emit(OpCodes.Stobj, memberType);

        while (variablesStack.Count > 0)
        {
            var (assignedCacheVar, fieldRef, property) = variablesStack.Pop();

            ilGenerator.Emit(OpCodes.Ldloc, fieldRef);
            ilGenerator.Emit(OpCodes.Ldloc, assignedCacheVar);
            ilGenerator.Emit(OpCodes.Call, property.SetMethod!);
        }

        ilGenerator.Emit(OpCodes.Ret);
        return dynMethod.CreateDelegate(typeof(Setter<>).MakeGenericType(typeof(Entity), memberType));
    }

    private class DynamicProperty<ValueT>(Getter<ValueT> getter, Setter<ValueT> setter) : IProperty<Entity, ValueT>
    {
        public ValueT Get(in Entity obj) => getter.Invoke(in obj);

        public void Set(ref Entity obj, in ValueT value) => setter.Invoke(ref obj, in value);
    }

    protected override (IProperty<Entity>, Type) ParsePropertyImpl(string property)
    {
        var parts = property.Split("::");
        var componentTypeName = parts[0];
        var memberPath = parts[1];

        // 查找组件和成员类型
        var componentType = GetType(componentTypeName, ComponentTypes);
        var memberType = GetMemberType(componentType, memberPath);

        // 编译属性对象
        var getter = CompileGetter(componentType, memberType, memberPath);
        var setter = CompileSetter(componentType, memberType, memberPath);
        var propertyInstance = (IProperty<Entity>)
            Activator.CreateInstance(typeof(DynamicProperty<>).MakeGenericType(memberType), getter, setter)!;

        return (propertyInstance, memberType);
    }

    #endregion
}
