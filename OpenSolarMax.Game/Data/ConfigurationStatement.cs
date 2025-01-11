namespace OpenSolarMax.Game.Data;

public record ConfigurationStatement(
    string Key,
    string[] Bases,
    IEntityConfiguration[] Configurations
);
