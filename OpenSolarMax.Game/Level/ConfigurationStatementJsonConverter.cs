using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSolarMax.Game.Modding.Configuration;

namespace OpenSolarMax.Game.Level;

/// <summary>
///
/// </summary>
/// <param name="schemaNamesByConfigurationId">从配置索引到配置模式名称的映射</param>
/// <param name="schemaInfosBySchemaName">从配置模式名称到配置模式类型的映射</param>
internal class ConfigurationStatementJsonConverter(
    IReadOnlyDictionary<string, string> schemaNamesByConfigurationId,
    IReadOnlyDictionary<string, ConfigurationInfo> schemaInfosBySchemaName
) : JsonConverter<ConfigurationStatement>
{
    public override ConfigurationStatement? Read(ref Utf8JsonReader reader, Type typeToConvert,
                                                 JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        var baseProp = element.GetProperty("$base");
        var baseConfigurationIds = baseProp.ValueKind switch
        {
            JsonValueKind.String => [baseProp.GetString()!],
            JsonValueKind.Array => baseProp.EnumerateArray().Select(x => x.GetString()!).ToArray(),
            _ => throw new JsonException(),
        };
        var schemaName = schemaNamesByConfigurationId[baseConfigurationIds[0]];
        if (baseConfigurationIds.Any(b => schemaNamesByConfigurationId[b] != schemaName))
            throw new Exception("All base configuration should have the same configuration key");

        var configuration = (IConfiguration)element.Deserialize(schemaInfosBySchemaName[schemaName].Type, options)!;

        return new ConfigurationStatement(schemaName,
                                          [..baseConfigurationIds.Where(b => b != schemaName)],
                                          configuration);
    }

    public override void Write(Utf8JsonWriter writer, ConfigurationStatement value,
                               JsonSerializerOptions options)
        => throw new NotImplementedException();
}
