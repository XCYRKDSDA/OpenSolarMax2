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
    private readonly List<object> _lateUpdate1Systems = [];
    private readonly List<ICalcSystem> _lateUpdate2Systems = [];

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
        var lateUpdate1Systems = sortedSystemTypes
            .LateUpdate1Systems.Select(t =>
                PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();
        var lateUpdate2Systems = sortedSystemTypes
            .LateUpdate2Systems.Select(t =>
                PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();

        // 注册挂载点（需所有系统实例）
        RegisterHook(
            updateSystems.Concat(lateUpdate1Systems).Concat(lateUpdate2Systems),
            hookImplInfos
        );

        _updateSystems.AddRange(updateSystems.Cast<ITickSystem>());
        _lateUpdate1Systems.AddRange(lateUpdate1Systems);
        _lateUpdate2Systems.AddRange(lateUpdate2Systems.Cast<ICalcSystem>());
    }

    public void Update(GameTime gameTime)
    {
        Debug.Assert(_commandBuffer.Size == 0);

        // 执行积分系统
        foreach (var system in _updateSystems)
        {
            // Debug.WriteLine($"Update {system.GetType().Name}");
            system.Update(gameTime);
        }

        LateUpdate();
    }

    public void LateUpdate()
    {
        Debug.WriteLine("Start late update");
        // 不动点迭代：随动系统反复执行直到无结构化变更
        for (var iteration = 0; ; iteration++)
        {
            Debug.Assert(_commandBuffer.Size == 0);
            foreach (var system in _lateUpdate1Systems)
            {
                // Debug.WriteLine($"LateUpdate1 {system.GetType().Name}");
                if (system is ICalcSystemWithStructuralChanges withChanges)
                    withChanges.Update(_commandBuffer);
                else if (system is ICalcSystem calc)
                    calc.Update();
            }

            var structuralChanges = _commandBuffer.Size;
            var hadStructuralChanges = _commandBuffer.Size > 0;
            _commandBuffer.Playback(_world, dispose: true);

            // 如果无结构化变更，则退出循环
            if (!hadStructuralChanges)
                break;

            Debug.WriteLine($"Continue iteration because of {structuralChanges} changes");

            // 如果迭代次数太多，则抛异常
            if (iteration >= MaxFixpointIterations)
                throw new Exception(
                    $"fixpoint did not converge within {MaxFixpointIterations} iterations"
                );
        }

        Debug.Assert(_commandBuffer.Size == 0);

        // 执行 LateUpdate2 阶段系统
        foreach (var system in _lateUpdate2Systems)
        {
            // Debug.WriteLine($"LateUpdate2 {system.GetType().Name}");
            system.Update();
        }

        Debug.Assert(_commandBuffer.Size == 0);
    }

    public void Dispose()
    {
        // 释放 CommandBuffer
        _commandBuffer.Dispose();

        // 释放所有内部系统
        foreach (
            var sys in _updateSystems
                .Concat<object>(_lateUpdate1Systems)
                .Concat(_lateUpdate2Systems)
        )
        {
            if (sys is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
