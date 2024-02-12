namespace OpenSolarMax.Game.Data;

internal class LevelStatement(string? @base, IEntityConfiguration[] configs)
{
    public string? Base { get; } = @base;

    public IEntityConfiguration[] Configs { get; } = configs;
}
