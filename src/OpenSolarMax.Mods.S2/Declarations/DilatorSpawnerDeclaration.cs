using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

[SchemaName("dilator_spawner")]
public class DilatorSpawnerDeclaration : IDeclaration<DilatorSpawnerDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Team { get; set; }

    /// <summary>
    /// 出现时为临时 dilator 生成的舰船数
    /// </summary>
    public int? Ships { get; set; }

    /// <summary>
    /// 总人口容量触发阈值
    /// </summary>
    public int? TotalPopulationThreshold { get; set; }

    /// <summary>
    /// 总现存人口触发阈值
    /// </summary>
    public int? CurrentPopulationThreshold { get; set; }

    public DilatorSpawnerDeclaration Aggregate(DilatorSpawnerDeclaration newCfg)
    {
        return new DilatorSpawnerDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Team = newCfg.Team ?? Team,
            Ships = newCfg.Ships ?? Ships,
            TotalPopulationThreshold = newCfg.TotalPopulationThreshold ?? TotalPopulationThreshold,
            CurrentPopulationThreshold =
                newCfg.CurrentPopulationThreshold ?? CurrentPopulationThreshold,
        };
    }
}

[Translate("dilator_spawner", ConceptNames.DilatorSpawner)]
public class DilatorSpawnerDeclarationTranslator
    : ITranslator<DilatorSpawnerDeclaration, DilatorSpawnerDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public DilatorSpawnerDescription ToDescription(
        DilatorSpawnerDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new DilatorSpawnerDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Team is not null)
            desc.Team = otherEntities[declaration.Team];
        if (declaration.Ships is not null)
            desc.ShipCount = declaration.Ships.Value;
        if (declaration.TotalPopulationThreshold is not null)
            desc.TotalPopulationThreshold = declaration.TotalPopulationThreshold.Value;
        if (declaration.CurrentPopulationThreshold is not null)
            desc.CurrentPopulationThreshold = declaration.CurrentPopulationThreshold.Value;

        return desc;
    }
}
