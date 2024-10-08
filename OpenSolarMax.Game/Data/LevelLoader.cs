﻿using System.Text.Json;
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

        var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, IncludeFields = true };
        // 添加基础类型转换器
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new Vector2JsonConverter());
        options.Converters.Add(new ColorJsonConverter());
        options.Converters.Add(new BlendStateJsonConverter());
        // 添加资源引用转换器
        var directory = path.GetDirectory();
        options.Converters.Add(new AssetReferenceJsonConverter<TextureRegion>(assets, directory));

        var jsonLevel = JsonSerializer.Deserialize<JsonLevel>(stream, options) ?? throw new JsonException();

        // 将缺省的配置语句的类型记录到缓存中
        var templateConfigurationKeysCache = new Dictionary<string, string>();
        foreach (var key in ConfigurationTypes.Keys)
            templateConfigurationKeysCache.Add(key, key);

        var level = new Level();

        // 解析模板语句
        foreach (var (templateName, templateElement) in jsonLevel.Templates)
        {
            // 根据base字段寻找配置类型
            var baseProp = templateElement.GetProperty("$base");
            var bases = baseProp.ValueKind switch
            {
                JsonValueKind.String => [baseProp.GetString()!],
                JsonValueKind.Array => baseProp.EnumerateArray().Select(x => x.GetString()!).ToArray(),
                _ => throw new JsonException(),
            };
            var configurationKey = templateConfigurationKeysCache[bases[0]];
            if (bases.Any(@base => templateConfigurationKeysCache[@base] != configurationKey))
                throw new Exception("All base template should have the same configuration key");

            // 从json文件解析当前模板语句本身的配置
            var configs =
                (from configuratorType in ConfigurationTypes[configurationKey]
                 select (IEntityConfiguration)templateElement.Deserialize(configuratorType, options)!
                ).ToArray();

            // 构造并添加新的模板语句
            level.Templates.Add(templateName,
                                new LevelStatement(bases.Where(@base => @base != configurationKey).ToArray(), configs));

            // 将该模板语句的配置类型加入到缓存中
            templateConfigurationKeysCache.Add(templateName, configurationKey);
        }

        // 解析实体语句
        foreach (var entityElement in jsonLevel.Entities)
        {
            // 根据base字段寻找配置类型
            var baseProp = entityElement.GetProperty("$base");
            var bases = baseProp.ValueKind switch
            {
                JsonValueKind.String => [baseProp.GetString()!],
                JsonValueKind.Array => baseProp.EnumerateArray().Select(x => x.GetString()!).ToArray(),
                _ => throw new JsonException(),
            };
            var configurationKey = templateConfigurationKeysCache[bases[0]];
            if (bases.Any(@base => templateConfigurationKeysCache[@base] != configurationKey))
                throw new Exception("All base template should have the same configuration key");

            // 从json文件解析当前实体语句本身的配置
            var configs =
                (from configuratorType in ConfigurationTypes[configurationKey]
                 select (IEntityConfiguration)entityElement.Deserialize(configuratorType, options)!
                ).ToArray();

            // 获取id, 如果有的话
            var id = entityElement.TryGetProperty("$id", out var idProp) ? idProp.GetString() : null;

            // 获取实体构建个数
            int num = entityElement.TryGetProperty("$num", out var numProp) ? numProp.GetInt32() : 1;

            // 构造并添加新的实体语句
            level.Entities.Add(
                (id,
                 new LevelStatement(bases.Where(@base => @base != configurationKey).ToArray(), configs),
                 num)
            );
        }

        return level;
    }
}
