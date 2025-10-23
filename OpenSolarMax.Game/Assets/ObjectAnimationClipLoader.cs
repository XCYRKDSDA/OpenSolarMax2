using System.Reflection;
using System.Reflection.Emit;
using Nine.Animations;
using Nine.Assets;

namespace OpenSolarMax.Game.Assets;

internal class ObjectAnimationClipLoader<T> : AnimationClipLoaderBase<T>
{
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

    private static Delegate CompileGetter(Type memberType, string memberPath)
    {
        var dynMethod = new DynamicMethod("GetMember", memberType, [typeof(T).MakeByRefType()]);
        var ilGenerator = dynMethod.GetILGenerator();
        var localVariablesCount = 0;

        ilGenerator.Emit(OpCodes.Ldarg, 0);

        var typeSoFar = typeof(T);
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

        return dynMethod.CreateDelegate(typeof(Getter<>).MakeGenericType(typeof(T), memberType));
    }

    private static Delegate CompileSetter(Type memberType, string memberPath)
    {
        var dynMethod =
            new DynamicMethod("SetMember", null, [typeof(T).MakeByRefType(), memberType.MakeByRefType()]);
        var ilGenerator = dynMethod.GetILGenerator();
        var variablesStack = new Stack<(LocalBuilder, LocalBuilder, PropertyInfo)>(); // 属性缓存、属性属于的对象的引用、属性信息

        ilGenerator.Emit(OpCodes.Ldarg, 0);

        var typeSoFar = typeof(T);
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
                if (!fieldRef.LocalType.IsValueType) ilGenerator.Emit(OpCodes.Ldind_Ref);
                ilGenerator.Emit(OpCodes.Callvirt, property.GetMethod!);
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
            if (!fieldRef.LocalType.IsValueType) ilGenerator.Emit(OpCodes.Ldind_Ref);
            ilGenerator.Emit(OpCodes.Ldloc, assignedCacheVar);
            ilGenerator.Emit(OpCodes.Callvirt, property.SetMethod!);
        }

        ilGenerator.Emit(OpCodes.Ret);
        return dynMethod.CreateDelegate(typeof(Setter<>).MakeGenericType(typeof(T), memberType));
    }

    private class DynamicProperty<ValueT>(Getter<ValueT> getter, Setter<ValueT> setter) : IProperty<T, ValueT>
    {
        public ValueT Get(in T obj) => getter.Invoke(in obj);

        public void Set(ref T obj, in ValueT value) => setter.Invoke(ref obj, in value);
    }

    protected override (IProperty<T>, Type) ParsePropertyImpl(string property)
    {
        // 查找成员类型
        var memberType = GetMemberType(typeof(T), property);

        // 编译属性对象
        var getter = CompileGetter(memberType, property);
        var setter = CompileSetter(memberType, property);
        var propertyInstance = (IProperty<T>)
            Activator.CreateInstance(typeof(DynamicProperty<>).MakeGenericType(typeof(T), memberType), getter, setter)!;

        return (propertyInstance, memberType);
    }
}
