namespace OpenSolarMax.Game.Level;

internal class LevelFile
{
    public Dictionary<string, DeclarationStatement> Templates { get; } = [];

    public List<(string? Id, DeclarationStatement Statement, int Num)> Entities { get; } = [];
}
