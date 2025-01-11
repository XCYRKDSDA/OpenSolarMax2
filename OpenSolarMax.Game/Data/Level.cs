namespace OpenSolarMax.Game.Data;

internal class Level
{
    public Dictionary<string, ConfigurationStatement> Templates { get; } = [];

    public List<(string? Id, ConfigurationStatement Statement, int Num)> Entities { get; } = [];
}
