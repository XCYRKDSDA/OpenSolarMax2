using System.Collections.Immutable;
using OpenSolarMax.Game.Modding.Declaration;

namespace OpenSolarMax.Game.Level;

/// <summary>
/// 记录关卡文件中一个对属于某种概念的对象的描述语句
/// </summary>
/// <param name="SchemaName">该语句所采用的配置结构的名称</param>
/// <param name="BaseDecalarationIds">该语句所基于的其他描述配置的索引</param>
/// <param name="Declaration">该语句直接对概念描述的配置</param>
public record DeclarationStatement(
    string SchemaName,
    ImmutableArray<string> BaseDecalarationIds,
    IDeclaration Declaration
);
