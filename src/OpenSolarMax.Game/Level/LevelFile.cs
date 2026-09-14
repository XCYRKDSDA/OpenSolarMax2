using Microsoft.Extensions.Configuration;

namespace OpenSolarMax.Game.Level;

internal class LevelFile
{
    public required Dictionary<string, DeclarationStatement> Templates { get; init; }

    public required List<(string? Id, DeclarationStatement Statement)> Entities { get; init; }

    public IConfiguration? Configs { get; init; }
}
