using System.Diagnostics;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

public class DualStageAggregateSystem : ISystem
{
    private readonly World _world;

    private readonly List<ISystem> _coreUpdateSystems;
    private readonly List<IStructuralChangeSystem> _structuralChangeSystems;
    private readonly List<IStructuralChangeSystem> _reactivelyStructuralChangeSystems;
    private readonly List<ISystem> _lateUpdateSystems;

    private readonly CommandBuffer _commandBuffer = new();

    public DualStageAggregateSystem(World world, ICollection<Type> systemTypes,
                                    IReadOnlyDictionary<Type, object> @params)
    {
        _world = world;

        // 区分四类系统

        var coreUpdateSystemTypes = new List<Type>();
        var structuralChangeSystemTypes = new List<Type>();
        var reactivelyStructuralChangeSystemTypes = new List<Type>();
        var lateUpdateSystemTypes = new List<Type>();

        foreach (var systemType in systemTypes)
        {
            // 指定了 Stage1 的系统为 CoreUpdateSystem
            if (systemType.GetCustomAttribute<Stage1Attribute>() is not null)
                coreUpdateSystemTypes.Add(systemType);
            // 指定了 Stage2 的系统为 LateUpdateSystem，但是具体类型还要再判断
            else if (systemType.GetCustomAttribute<Stage2Attribute>() is not null)
            {
                // 指定了 CreateEntities 和 DestroyEntities 的系统为 StructuralChangeSystem，但是具体类型还要再判断
                if (systemType.GetCustomAttribute<CreateEntitiesAttribute>() is not null ||
                    systemType.GetCustomAttribute<DestroyEntitiesAttribute>() is not null)
                {
                    // 指定了任意读取实体的 Read 的系统为 ReactivelyStructuralChangeSystem
                    if (systemType.GetCustomAttributes<ReadAttribute>().Any(r => r.WithEntities))
                        reactivelyStructuralChangeSystemTypes.Add(systemType);
                    // 其余为普通的 StructuralChangeSystem
                    else
                        structuralChangeSystemTypes.Add(systemType);
                }
                // 其余为普通的 LateUpdateSystem
                else
                    lateUpdateSystemTypes.Add(systemType);
            }
            // 若未指定 Stage，默认为 Stage1
            else
                coreUpdateSystemTypes.Add(systemType);
        }

        // 获取各组系统的顺序
        var coreUpdateSystemExecutionOrders =
            Moddings.ExtractExecutionOrders(coreUpdateSystemTypes, ReadReference.LastFrame);
        var structuralChangeSystemExecutionOrders =
            Moddings.ExtractExecutionOrders(structuralChangeSystemTypes, ReadReference.NextFrame);
        var reactivelyStructuralChangeSystemExecutionOrders =
            Moddings.ExtractExecutionOrders(reactivelyStructuralChangeSystemTypes, ReadReference.NextFrame);
        var lateUpdateSystemExecutionOrders =
            Moddings.ExtractExecutionOrders(lateUpdateSystemTypes, ReadReference.NextFrame);

        // 拓扑排序
        var sortedCoreUpdateSystemTypes =
            Moddings.TopologicalSortSystems(coreUpdateSystemExecutionOrders);
        var sortedStructuralChangeSystemTypes =
            Moddings.TopologicalSortSystems(structuralChangeSystemExecutionOrders);
        var sortedReactivelyStructuralChangeSystemTypes =
            Moddings.TopologicalSortSystems(reactivelyStructuralChangeSystemExecutionOrders);
        var sortedLateUpdateSystemTypes =
            Moddings.TopologicalSortSystems(lateUpdateSystemExecutionOrders);

        // 实例化
        _coreUpdateSystems =
            sortedCoreUpdateSystemTypes
                .Select(type => Moddings.CreateSystem<ISystem>(type, world, @params)).ToList();
        _structuralChangeSystems =
            sortedStructuralChangeSystemTypes
                .Select(type => Moddings.CreateSystem<IStructuralChangeSystem>(type, world, @params)).ToList();
        _reactivelyStructuralChangeSystems =
            sortedReactivelyStructuralChangeSystemTypes
                .Select(type => Moddings.CreateSystem<IStructuralChangeSystem>(type, world, @params)).ToList();
        _lateUpdateSystems =
            sortedLateUpdateSystemTypes
                .Select(type => Moddings.CreateSystem<ISystem>(type, world, @params)).ToList();
    }

    public void Initialize()
    {
        foreach (var system in _coreUpdateSystems)
            system.Initialize();

        Debug.Assert(_commandBuffer.Size == 0);
        foreach (var system in _structuralChangeSystems)
            system.Initialize(_commandBuffer);

        while (_commandBuffer.Size > 0)
        {
            _commandBuffer.Playback(_world, dispose: true);
            foreach (var system in _reactivelyStructuralChangeSystems)
                system.Initialize(_commandBuffer);
        }

        foreach (var system in _lateUpdateSystems)
            system.Initialize();
    }

    public void Update(GameTime gameTime)
    {
        foreach (var system in _coreUpdateSystems)
            system.Update(gameTime);

        Debug.Assert(_commandBuffer.Size == 0);
        foreach (var system in _structuralChangeSystems)
            system.Update(gameTime, _commandBuffer);

        while (_commandBuffer.Size > 0)
        {
            _commandBuffer.Playback(_world, dispose: true);
            foreach (var system in _reactivelyStructuralChangeSystems)
                system.Update(gameTime, _commandBuffer);
        }

        foreach (var system in _lateUpdateSystems)
            system.Update(gameTime);
    }
}
