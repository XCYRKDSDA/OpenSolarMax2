namespace OpenSolarMax.Game.Level;

internal class LevelFile
{
    public Dictionary<string, ConfigurationStatement> Templates { get; } = [];

    public List<(string? Id, ConfigurationStatement Statement, int Num)> Entities { get; } = [];
}
