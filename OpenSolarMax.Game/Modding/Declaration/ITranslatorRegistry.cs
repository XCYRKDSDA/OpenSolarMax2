using System.Diagnostics.CodeAnalysis;

namespace OpenSolarMax.Game.Modding.Declaration;

public interface ITranslatorRegistry
{
    /// <summary>
    /// 根据 Schema 名称查找对应的内部 Concept 名称和负责将 Declaration 转换成内部 Description 的 Translator 对象
    /// </summary>
    /// <param name="schemaName">查找 Translator 的 Schema 名称</param>
    /// <returns>查找结果元组, 包括 Concept 名称和 Translator 对象</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    (string ConceptName, ITranslator translator) GetBySchema(string schemaName);

    /// <summary>
    /// 尝试根据 Schema 名称查找对应的内部 Concept 名称和负责将 Declaration 转换成内部 Description 的 Translator 对象.
    /// 若查找失败, 将返回 <c>false</c>
    /// </summary>
    /// <param name="schemaName">查找 Translator 的 Schema 名称</param>
    /// <param name="conceptName">内部 Concept 名称</param>
    /// <param name="translator">Translator 对象</param>
    /// <returns></returns>
    bool TryGetBySchema(string schemaName,
                        [MaybeNullWhen(false)] out string conceptName,
                        [MaybeNullWhen(false)] out ITranslator translator);
}
