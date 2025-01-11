using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSolarMax.Game.Data;

internal class ConfigurationStatementJsonConverter(
    IReadOnlyDictionary<string, string> templateKeysCache,
    IReadOnlyDictionary<string, Type[]> configurationTypes
) : JsonConverter<ConfigurationStatement>
{
    public override ConfigurationStatement? Read(ref Utf8JsonReader reader, Type typeToConvert,
                                                 JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        var baseProp = element.GetProperty("$base");
        var bases = baseProp.ValueKind switch
        {
            JsonValueKind.String => [baseProp.GetString()!],
            JsonValueKind.Array => baseProp.EnumerateArray().Select(x => x.GetString()!).ToArray(),
            _ => throw new JsonException(),
        };
        var configurationKey = templateKeysCache[bases[0]];
        if (bases.Any(@base => templateKeysCache[@base] != configurationKey))
            throw new Exception("All base template should have the same configuration key");

        var configs = configurationTypes[configurationKey].Select(
            type => (IEntityConfiguration)element.Deserialize(type, options)!
        ).ToArray();
        return new(configurationKey, bases.Where(@base => @base != configurationKey).ToArray(), configs);
    }

    public override void Write(Utf8JsonWriter writer, ConfigurationStatement value,
                               JsonSerializerOptions options)
        => throw new NotImplementedException();
}
