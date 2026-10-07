using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Components;

[Component]
public struct DilatorConversionState : ICountUpTimer
{
    /// <summary>
    /// 从事件创建起的经过时长
    /// </summary>
    public TimeSpan TimeElapsed { get; set; }

    /// <summary>
    /// 第一段光带已创建
    /// </summary>
    public bool RampBurstSpawned;

    /// <summary>
    /// 慢速收尾光斑已创建
    /// </summary>
    public bool SlowBlobSpawned;

    /// <summary>
    /// 第二段光带已创建
    /// </summary>
    public bool SustainBurstSpawned;

    /// <summary>
    /// 快速收尾光斑已创建
    /// </summary>
    public bool FastBlobSpawned;

    /// <summary>
    /// 中场演出与转化波已放出
    /// </summary>
    public bool WaveReleased;
}
