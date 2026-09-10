using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Nine.Assets;
using Nine.Assets.Serialization;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Utils;
using Zio;

namespace OpenSolarMax.Game.Level;

internal class LevelLoader(
    IReadOnlyDictionary<string, DeclarationSchemaInfo> declarationSchemaInfos
) : IAssetLoader<LevelFile>
{
    private class JsonLevel
    {
        public List<string> Includes { get; set; } = [];

        public Dictionary<string, JsonElement> Templates { get; set; } = [];

        public JsonElement[] Entities { get; set; } = [];

        public JsonElement Player { get; set; }

        public JsonElement? Configs { get; set; }
    }

    private static void CollectJsonLevels(
        IFileSystem fs,
        UPath path,
        HashSet<UPath> visited,
        List<(UPath Path, JsonLevel Level)> collected
    )
    {
        // 必须在处理 includes 之前登记，防止循环引用导致无限递归
        if (!visited.Add(path))
            return;

        using var stream = fs.OpenFile(path, FileMode.Open, FileAccess.Read);

        var basicSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
        };
        var jsonLevel =
            JsonSerializer.Deserialize<JsonLevel>(stream, basicSerializerOptions)
            ?? throw new JsonException();

        // 先处理被包含的文件，使其模板和实体位于当前文件内容之前
        foreach (var includePath in jsonLevel.Includes)
        {
            CollectJsonLevels(
                fs,
                UPath.Combine(path.GetDirectory(), includePath),
                visited,
                collected
            );
        }

        collected.Add((path, jsonLevel));
    }

    private LevelFile ParseAndMerge(List<(UPath Path, JsonLevel Level)> collected)
    {
        // 初始化从配置模式索引到配置模式名称的映射
        var schemaNamesByDeclarationId = declarationSchemaInfos.Keys.ToDictionary(key => key);

        var statementSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
        };
        // 添加基础类型转换器
        statementSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        statementSerializerOptions.Converters.Add(new Vector2JsonConverter());
        statementSerializerOptions.Converters.Add(new ColorJsonConverter());
        statementSerializerOptions.Converters.Add(new BlendStateJsonConverter());
        // 添加 OneOf 转换器
        statementSerializerOptions.Converters.Add(new OneOfJsonConverterFactory());
        // 添加语句转换器
        statementSerializerOptions.Converters.Add(
            new DeclarationStatementJsonConverter(
                schemaNamesByDeclarationId,
                declarationSchemaInfos
            )
        );

        var templates = new Dictionary<string, DeclarationStatement>();
        var entities = new List<(string? Id, DeclarationStatement Statement)>();
        var configBuilder = new ConfigurationBuilder();

        foreach (var (path, jsonLevel) in collected)
        {
            // 解析模板语句
            foreach (var (templateKey, templateJsonElement) in jsonLevel.Templates)
            {
                if (templates.ContainsKey(templateKey))
                    throw new Exception($"模板 \"{templateKey}\" 重复定义（文件：{path}）");

                var statement = templateJsonElement.Deserialize<DeclarationStatement>(
                    statementSerializerOptions
                )!;

                // 构造并添加新的模板语句
                templates.Add(templateKey, statement);

                // 将该模板语句的配置类型加入到缓存中
                schemaNamesByDeclarationId[templateKey] = statement.SchemaName;
            }

            // 解析实体语句
            foreach (var entityJsonElement in jsonLevel.Entities)
            {
                var statement = entityJsonElement.Deserialize<DeclarationStatement>(
                    statementSerializerOptions
                )!;

                // 获取id, 如果有的话
                var id = entityJsonElement.TryGetProperty("$id", out var idProp)
                    ? idProp.GetString()
                    : null;

                // 构造并添加新的实体语句
                entities.Add((id, statement));
            }

            // 将该文件的配置转换为 IConfiguration 后叠加
            if (jsonLevel.Configs is { } configs)
            {
                using var configStream = new MemoryStream(
                    System.Text.Encoding.UTF8.GetBytes(configs.GetRawText())
                );
                configBuilder.AddConfiguration(
                    new ConfigurationBuilder().AddJsonStream(configStream).Build()
                );
            }
        }

        return new LevelFile
        {
            Templates = templates,
            Entities = entities,
            Configs = configBuilder.Sources.Count > 0 ? configBuilder.Build() : null,
        };
    }

    public LevelFile Load(IFileSystem fs, IAssetsManager assets, in UPath path)
    {
        // 收集阶段：只加载文件骨架，得到"被包含文件在前"的有序序列
        var collected = new List<(UPath Path, JsonLevel Level)>();
        var visited = new HashSet<UPath>(); // 记录已经加载过的文件，避免重复加载和循环引用
        CollectJsonLevels(fs, path, visited, collected);

        // 解析阶段：按序解析各文件的模板、实体与配置，并合并到最终结果
        return ParseAndMerge(collected);
    }
}
