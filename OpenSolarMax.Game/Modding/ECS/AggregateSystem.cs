using System.Diagnostics;
using System.Linq;
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
    private readonly List<IReactiveSystem> _reactiveSystems = [];

    private readonly CommandBuffer[] _buffers = [new(), new()];
    private int _currentIndex;

    public AggregateSystem(
        World world,
        ImmutableSortedSystemTypesCollection sortedSystemTypes,
        IReadOnlyDictionary<Type, object> @params,
        IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> hookImplInfos
    )
    {
        _world = world;
        var eventRegistry = new EventRegistry(world, this);

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
        var reactiveSystems = sortedSystemTypes
            .ReactiveSystems.Select(t =>
                (IReactiveSystem)
                    PluginFactory.Instantiate(t, [(typeof(EventRegistry), eventRegistry)], @params)
            )
            .ToList();

        // 注册挂载点（需所有系统实例）
        RegisterHook(
            updateSystems
                .Concat(lateUpdate1Systems)
                .Concat(lateUpdate2Systems)
                .Concat(reactiveSystems),
            hookImplInfos
        );

        _updateSystems.AddRange(updateSystems.Cast<ITickSystem>());
        _lateUpdate1Systems.AddRange(lateUpdate1Systems);
        _lateUpdate2Systems.AddRange(lateUpdate2Systems.Cast<ICalcSystem>());
        _reactiveSystems.AddRange(reactiveSystems);
    }

    internal CommandBuffer CurrentCommandBuffer => _buffers[_currentIndex];

    public void Update(GameTime gameTime)
    {
        Debug.Assert(_buffers.All(b => b.Size == 0));

        // 执行积分系统
        foreach (var system in _updateSystems)
        {
            system.Update(gameTime);
        }

        LateUpdate();
    }

    public void LateUpdate()
    {
        // 不动点迭代：随动系统反复执行直到无结构化变更
        // 外循环：执行 LateUpdate1 系统，若产生结构变更则经内循环排空后再次执行
        for (var iteration = 0; ; iteration++)
        {
            // 如果迭代次数太多，则抛异常（上限 32 次系统执行）
            if (iteration >= MaxFixpointIterations)
                throw new Exception(
                    $"fixpoint did not converge within {MaxFixpointIterations} iterations"
                );

            Debug.Assert(_buffers[_currentIndex ^ 1].Size == 0);

            // 执行 LateUpdate1 阶段系统：带结构变更的系统写入 Buffered，其余直接执行
            foreach (var system in _lateUpdate1Systems)
            {
                if (system is ICalcSystemWithStructuralChanges withChanges)
                    withChanges.Update(CurrentCommandBuffer);
                else if (system is ICalcSystem calc)
                    calc.Update();
            }

            // 若系统没有写入任何结构变更，则已收敛，退出循环
            if (_buffers[_currentIndex].Size == 0)
                break;

            // 内循环：反复 Playback 直到 Buffered 排空（上限 32 次执行）
            for (var playbackIteration = 0; _buffers[_currentIndex].Size > 0; playbackIteration++)
            {
                // 如果迭代次数太多，则抛异常
                if (playbackIteration >= MaxFixpointIterations)
                    throw new Exception(
                        $"fixpoint did not converge within {MaxFixpointIterations} iterations"
                    );

                // 切换当前写入目标：播放期间回调写入另一个 buffer
                // （_currentIndex 在 0/1 间切换，^ 1 即取另一个 buffer）
                _currentIndex ^= 1;
                _buffers[_currentIndex ^ 1].Playback(_world, dispose: true);
            }

            // 退出时两个 buffer 必空
            Debug.Assert(_buffers.All(b => b.Size == 0));
        }

        Debug.Assert(_buffers.All(b => b.Size == 0));

        // 执行 LateUpdate2 阶段系统
        foreach (var system in _lateUpdate2Systems)
            system.Update();

        Debug.Assert(_buffers.All(b => b.Size == 0));
    }

    public void Dispose()
    {
        _buffers[0].Dispose();
        _buffers[1].Dispose();

        // 释放所有内部系统
        foreach (
            var sys in _updateSystems
                .Concat<object>(_lateUpdate1Systems)
                .Concat(_lateUpdate2Systems)
                .Concat(_reactiveSystems)
        )
        {
            if (sys is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
