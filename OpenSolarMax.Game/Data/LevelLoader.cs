using System.Text.Json;
using System.Text.Json.Serialization;
using Nine.Assets;
using Nine.Assets.Serialization;
using Nine.Graphics;
using Zio;

namespace OpenSolarMax.Game.Data;

internal class LevelLoader : IAssetLoader<Level>
{
    private class JsonLevel
    {
        public Dictionary<string, JsonElement> Templates { get; set; } = [];

        public JsonElement[] Entities { get; set; } = [];

        public JsonElement Player { get; set; }
    }

    public Dictionary<string, Type[]> ConfigurationTypes { get; set; } = [];

    public Level Load(IFileSystem fs, IAssetsManager assets, in UPath path)
    {
        using var stream = fs.OpenFile(path, FileMode.Open, FileAccess.Read);

        var basicSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        var jsonLevel = JsonSerializer.Deserialize<JsonLevel>(stream, basicSerializerOptions) ??
                        throw new JsonException();

        var statementSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        // 添加基础类型转换器
        statementSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        statementSerializerOptions.Converters.Add(new Vector2JsonConverter());
        statementSerializerOptions.Converters.Add(new ColorJsonConverter());
        statementSerializerOptions.Converters.Add(new BlendStateJsonConverter());
        // 添加资源引用转换器
        var directory = path.GetDirectory();
        statementSerializerOptions.Converters.Add(new AssetReferenceJsonConverter<TextureRegion>(assets, directory));

        // 将缺省的配置语句的类型记录到缓存中
        var templateConfigurationKeysCache = ConfigurationTypes.Keys.ToDictionary(key => key);
        // 添加语句转换器
        statementSerializerOptions.Converters.Add(
            new ConfigurationStatementJsonConverter(templateConfigurationKeysCache, ConfigurationTypes));

        var level = new Level();

        // 解析模板语句
        foreach (var (templateName, templateElement) in jsonLevel.Templates)
        {
            var statement = templateElement.Deserialize<ConfigurationStatement>(statementSerializerOptions)!;

            // 构造并添加新的模板语句
            level.Templates.Add(templateName, statement);

            // 将该模板语句的配置类型加入到缓存中
            templateConfigurationKeysCache.Add(templateName, statement.Key);
        }

        // 解析实体语句
        foreach (var entityElement in jsonLevel.Entities)
        {
            var statement = entityElement.Deserialize<ConfigurationStatement>(statementSerializerOptions)!;

            // 获取id, 如果有的话
            var id = entityElement.TryGetProperty("$id", out var idProp) ? idProp.GetString() : null;

            // 获取实体构建个数，如果有的话
            int num = entityElement.TryGetProperty("$num", out var numProp) ? numProp.GetInt32() : 1;

            // 构造并添加新的实体语句
            level.Entities.Add((id, statement, num));
        }

        return level;
    }
}
