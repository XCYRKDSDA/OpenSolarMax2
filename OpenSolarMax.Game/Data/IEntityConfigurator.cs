using Arch.Core;
using Archetype = OpenSolarMax.Core.Utils.Archetype;

namespace OpenSolarMax.Game.Data;

/// <summary>
/// 实体配置器的接口。提供了创建并配置一个实体的所有方法
/// </summary>
internal interface IEntityConfigurator
{
    /// <summary>
    /// 实体需要满足的原型
    /// </summary>
    Archetype Archetype { get; }

    /// <summary>
    /// 该配置器能够接受的配置数据类型
    /// </summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// 对已经按照指定原型分配好内存的实体进行初始化，使实体各项组件的数据满足该配置器所需的默认值
    /// </summary>
    /// <param name="entity"></param>
    void Initialize(in Entity entity);

    /// <summary>
    /// 根据具体的配置数据，对已经初始化过的实体进行配置
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="entity"></param>
    void Configure(IEntityConfiguration configuration, in Entity entity);
}
