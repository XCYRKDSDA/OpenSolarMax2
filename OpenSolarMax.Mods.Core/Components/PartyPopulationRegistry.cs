namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 该实体描述的阵营人口记录组件
/// </summary>
public struct PartyPopulationRegistry
{
    /// <summary>
    /// 当前阵营所有星球算在一起能够支撑的人口上限
    /// </summary>
    public int PopulationLimit;

    /// <summary>
    /// 当前阵营旗下所有单位已经占用的人口
    /// </summary>
    public int CurrentPopulation;
}
