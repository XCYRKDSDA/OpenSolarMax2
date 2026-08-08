using Arch.Buffer;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Modding.ECS;

public interface ITickSystem
{
    void Update(GameTime gameTime);
}

public interface ICalcSystem
{
    void Update();
}

public interface ICalcSystemWithStructuralChanges
{
    void Update(CommandBuffer commandBuffer);
}

/// <summary>
/// 响应式系统标记接口。只响应 Arch 事件、不执行 Update 的系统。
/// 订阅在构造函数中完成，框架只负责发现与实例化，不调用任何方法。
/// </summary>
public interface IReactiveSystem { }
