using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;

namespace OpenSolarMax.Game.Utils;

public class OneOfJsonConverter<T0, T1> : JsonConverter<OneOf<T0, T1>>
{
    public override OneOf<T0, T1> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        // Deserialize 行为可能会抛出意料外的 InvalidOperationException
        // 见 https://github.com/dotnet/runtime/issues/120798

        try
        {
            return OneOf<T0, T1>.FromT0(element.Deserialize<T0>(options)!);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }

        try
        {
            return OneOf<T0, T1>.FromT1(element.Deserialize<T1>(options)!);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { }

        throw new JsonException(
            $"此 JSON 值无法匹配 OneOf<{typeof(T0).Name}, {typeof(T1).Name}> 的任一类型"
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        OneOf<T0, T1> value,
        JsonSerializerOptions options
    ) => throw new NotImplementedException();
}

public class OneOfJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(OneOf<,>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var typeArgs = typeToConvert.GetGenericArguments();
        var converterType = typeof(OneOfJsonConverter<,>).MakeGenericType(typeArgs);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
