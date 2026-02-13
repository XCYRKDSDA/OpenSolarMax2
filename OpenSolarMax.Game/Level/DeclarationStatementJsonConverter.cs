using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSolarMax.Game.Modding.Declaration;

namespace OpenSolarMax.Game.Level;

/// <summary>
///
/// </summary>
/// <param name="schemaNamesByDeclarationId">从配置索引到配置模式名称的映射</param>
/// <param name="declarationSchemaInfos">从配置模式名称到配置模式类型的映射</param>
internal class DeclarationStatementJsonConverter(
    IReadOnlyDictionary<string, string> schemaNamesByDeclarationId,
    IReadOnlyDictionary<string, DeclarationSchemaInfo> declarationSchemaInfos
) : JsonConverter<DeclarationStatement>
{
    public override DeclarationStatement? Read(ref Utf8JsonReader reader, Type typeToConvert,
                                               JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        var baseProp = element.GetProperty("$base");
        var baseDeclarationIds = baseProp.ValueKind switch
        {
            JsonValueKind.String => [baseProp.GetString()!],
            JsonValueKind.Array => baseProp.EnumerateArray().Select(x => x.GetString()!).ToArray(),
            _ => throw new JsonException(),
        };
        var schemaName = schemaNamesByDeclarationId[baseDeclarationIds[0]];
        if (baseDeclarationIds.Any(b => schemaNamesByDeclarationId[b] != schemaName))
            throw new Exception("All base configuration should have the same configuration key");

        var configuration = (IDeclaration)element.Deserialize(declarationSchemaInfos[schemaName].Type, options)!;

        return new DeclarationStatement(schemaName,
                                        [..baseDeclarationIds.Where(b => b != schemaName)],
                                        configuration);
    }

    public override void Write(Utf8JsonWriter writer, DeclarationStatement value,
                               JsonSerializerOptions options)
        => throw new NotImplementedException();
}
