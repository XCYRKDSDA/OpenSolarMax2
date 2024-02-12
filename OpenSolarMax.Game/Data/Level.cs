namespace OpenSolarMax.Game.Data;

internal class Level
{
    public Dictionary<string, LevelStatement> Templates { get; } = [];

    public List<(string?, LevelStatement)> Entities { get; } = [];
}
