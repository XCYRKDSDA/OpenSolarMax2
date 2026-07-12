using System.Text.Json;

namespace OpenSolarMax.Game.Level;

internal class LevelFile
{
    public Dictionary<string, DeclarationStatement> Templates { get; } = [];

    public List<(string? Id, DeclarationStatement Statement)> Entities { get; } = [];

    public JsonElement? Configs { get; set; }
}
