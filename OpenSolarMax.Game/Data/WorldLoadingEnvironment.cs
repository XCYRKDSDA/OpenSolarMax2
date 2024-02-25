namespace OpenSolarMax.Game.Data;

/// <summary>
/// 世界加载过程中的环境。记录着加载某文件时的一些全局状态
/// </summary>
/// <param name="Configurators"></param>
public record class WorldLoadingEnvironment(
    ILookup<string, IEntityConfigurator> Configurators
);
