using System.Diagnostics;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Modding.ECS;

internal class AggregateSystem : IDisposable
{
    private const int MaxFixpointIterations = 32;

    private static void RegisterHook(
        IEnumerable<object> systems,
        IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> hookImplInfos
    )
    {
        // 收集所有的挂载点
        const BindingFlags hookFlags = BindingFlags.Public | BindingFlags.Instance;
        var hookPropertyInfos = systems
            .SelectMany(s => s.GetType().GetProperties(hookFlags), (s, p) => (obj: s, prop: p))
            .SelectMany(
                p => p.prop.GetCustomAttributes<HookAttribute>(),
                (p, a) => (hook: a.Name, p.obj, p.prop)
            );

        // 为每个挂载追加委托实现
        foreach (var (name, obj, prop) in hookPropertyInfos)
        {
            if (hookImplInfos.TryGetValue(name, out var implementations))
            {
                prop.SetValue(
                    obj,
                    implementations.Aggregate(
                        (Delegate)prop.GetValue(obj)!,
                        (d, m) => Delegate.Combine(d, m.CreateDelegate(prop.PropertyType))
                    )
                );
            }
        }
    }

    private readonly World _world;

    private readonly List<ITickSystem> _updateSystems = [];
    private readonly List<ICalcSystem> _preStructuralChangeSystems = [];
    private readonly List<ICalcSystemWithStructuralChanges> _structuralChangeSystems = [];
    private readonly List<ICalcSystem> _postStructuralChangeSystems = [];

    private readonly CommandBuffer _commandBuffer = new();

    public AggregateSystem(
        World world,
        ImmutableSortedSystemTypesCollection sortedSystemTypes,
        IReadOnlyDictionary<Type, object> @params,
        IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> hookImplInfos
    )
    {
        _world = world;

        var updateSystems = sortedSystemTypes
            .UpdateSystems.Select(t =>
                PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();
        var preStructuralChangeSystems = sortedSystemTypes
            .PreStructuralChangeSystems.Select(t =>
                PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();
        var structuralChangeSystems = sortedSystemTypes
            .StructuralChangeSystems.Select(t =>
                PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();
        var postStructuralChangeSystems = sortedSystemTypes
            .PostStructuralChangeSystems.Select(t =>
                PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();

        // 注册挂载点（需所有系统实例）
        RegisterHook(
            updateSystems
                .Concat(preStructuralChangeSystems)
                .Concat(structuralChangeSystems)
                .Concat(postStructuralChangeSystems),
            hookImplInfos
        );

        _updateSystems.AddRange(updateSystems.Cast<ITickSystem>());
        _preStructuralChangeSystems.AddRange(preStructuralChangeSystems.Cast<ICalcSystem>());
        _structuralChangeSystems.AddRange(
            structuralChangeSystems.Cast<ICalcSystemWithStructuralChanges>()
        );
        _postStructuralChangeSystems.AddRange(postStructuralChangeSystems.Cast<ICalcSystem>());
    }

    public void Update(GameTime gameTime)
    {
        Debug.Assert(_commandBuffer.Size == 0);

        // 执行积分系统
        foreach (var system in _updateSystems)
            system.Update(gameTime);

        LateUpdate();
    }

    public void LateUpdate()
    {
        Debug.Assert(_commandBuffer.Size == 0);

        // 不动点迭代：随动系统反复执行直到无结构化变更
        for (var iteration = 0; ; iteration++)
        {
            foreach (var system in _preStructuralChangeSystems)
                system.Update();

            foreach (var system in _structuralChangeSystems)
                system.Update(_commandBuffer);

            var hadStructuralChanges = _commandBuffer.Size > 0;
            _commandBuffer.Playback(_world, dispose: true);

            // 如果无结构化变更，则退出循环
            if (!hadStructuralChanges)
                break;

            // 如果迭代次数太多，则抛异常
            if (iteration >= MaxFixpointIterations)
                throw new Exception(
                    $"fixpoint did not converge within {MaxFixpointIterations} iterations"
                );
        }

        Debug.Assert(_commandBuffer.Size == 0);

        // 执行结构化变更后的随动系统
        foreach (var system in _postStructuralChangeSystems)
            system.Update();

        Debug.Assert(_commandBuffer.Size == 0);
    }

    public void Dispose()
    {
        // 释放 CommandBuffer
        _commandBuffer.Dispose();

        // 释放所有内部系统
        foreach (
            var sys in _updateSystems
                .Concat<object>(_preStructuralChangeSystems)
                .Concat(_structuralChangeSystems)
                .Concat(_postStructuralChangeSystems)
        )
        {
            if (sys is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
