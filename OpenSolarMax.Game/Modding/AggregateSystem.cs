using System.Diagnostics;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Modding;

internal class AggregateSystem
{
    private static object CreateSystem(Type type, World world, IReadOnlyDictionary<Type, object> @params)
    {
        var constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructorInfos.Length > 1)
            throw new Exception($"{type} has more than one public constructors!");
        if (constructorInfos.Length == 0)
            throw new Exception($"{type} has no public constructor!");
        var constructor = constructorInfos[0];

        var parameterInfos = constructor.GetParameters();
        if (parameterInfos[0].ParameterType != typeof(World))
            throw new Exception($"{type}'s constructor doesn't take Arch.Core.World as its first parameter!");

        var parameters = new object[parameterInfos.Length];
        parameters[0] = world;
        for (var i = 1; i < parameterInfos.Length; i++)
            parameters[i] = @params[parameterInfos[i].ParameterType];

        return constructor.Invoke(parameters);
    }

    private readonly World _world;

    private readonly List<object> _beforeStructuralChangesSystems = [];
    private readonly List<ICalcSystemWithStructuralChanges> _reactToStructuralChangeSystems = [];
    private readonly List<ICalcSystem> _afterStructuralChangesSystems = [];

    private readonly CommandBuffer _commandBuffer = new();

    public AggregateSystem(World world, IReadOnlyList<Type> sortedSystemTypes,
                           IReadOnlyDictionary<Type, object> @params,
                           IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> hookImplInfos)
    {
        _world = world;

        var systems = sortedSystemTypes.Select(t => CreateSystem(t, world, @params)).ToList();

        // 注册 hook
        Modding.RegisterHook(systems, hookImplInfos);

        // 寻找响应式结构化变更的部分，根据其划分为三部分
        foreach (var (type, system) in sortedSystemTypes.Zip(systems))
        {
            if (type.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
                _beforeStructuralChangesSystems.Add(system);
            else if (type.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any())
                _reactToStructuralChangeSystems.Add((ICalcSystemWithStructuralChanges)system);
            else if (type.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
                _afterStructuralChangesSystems.Add((ICalcSystem)system);
        }
    }

    private void LateUpdateImpl()
    {
        // 响应式结构化变更系统需要立刻执行
        foreach (var system in _reactToStructuralChangeSystems)
        {
            system.Update(_commandBuffer);
            _commandBuffer.Playback(_world);
        }

        foreach (var system in _afterStructuralChangesSystems)
            system.Update();
    }

    public void Update(GameTime gameTime)
    {
        Debug.Assert(_commandBuffer.Size == 0);

        foreach (var system in _beforeStructuralChangesSystems)
        {
            if (system is ITickSystem s1) s1.Update(gameTime);
            else if (system is ITickSystemWithStructuralChanges s2) s2.Update(gameTime, _commandBuffer);
            else if (system is ICalcSystem s3) s3.Update();
            else if (system is ICalcSystemWithStructuralChanges s4) s4.Update(_commandBuffer);
            else throw new Exception();
        }
        _commandBuffer.Playback(_world, dispose: true);

        LateUpdateImpl();
    }

    public void LateUpdate()
    {
        Debug.Assert(_commandBuffer.Size == 0);
        LateUpdateImpl();
    }
}
