using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace OpenSolarMax.Game.Modding.Declaration;

public class TranslatorsRegistry : ITranslatorRegistry
{
    private readonly ImmutableDictionary<
        string,
        (string ConceptName, ITranslator Translator)
    > _impl;

    public TranslatorsRegistry(
        IReadOnlyDictionary<string, DeclarationTranslatorInfo> translatorInfos
    )
    {
        _impl = translatorInfos.ToImmutableDictionary(
            kv => kv.Key,
            p =>
                (
                    p.Value.ConceptName,
                    (ITranslator)
                        PluginFactory.Instantiate(p.Value.Type, [], new Dictionary<Type, object>())
                )
        );
    }

    public (string ConceptName, ITranslator translator) GetBySchema(string schemaName) =>
        _impl[schemaName];

    public bool TryGetBySchema(
        string schemaName,
        [MaybeNullWhen(false)] out string conceptName,
        [MaybeNullWhen(false)] out ITranslator translator
    )
    {
        var result = _impl.TryGetValue(schemaName, out var pair);
        if (result)
        {
            (conceptName, translator) = pair;
            return true;
        }
        else
        {
            conceptName = null;
            translator = null;
            return false;
        }
    }
}
