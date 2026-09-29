using Arch.Core;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

[SchemaName("converting_dilator")]
public class ConvertingDilatorDeclaration
    : DilatorDeclaration,
        IDeclaration<ConvertingDilatorDeclaration>
{
    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public string? ConversionTeam { get; set; }

    /// <summary>
    /// 转化波半径的扩张速度
    /// </summary>
    public float? ConversionSpeed { get; set; }

    public ConvertingDilatorDeclaration Aggregate(ConvertingDilatorDeclaration newCfg)
    {
        return new ConvertingDilatorDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Team = newCfg.Team ?? Team,
            ProduceShips = newCfg.ProduceShips ?? ProduceShips,
            Ships = newCfg.Ships ?? Ships,
            Capital = newCfg.Capital ?? Capital,
            Garrison = newCfg.Garrison ?? Garrison,
            ConversionTeam = newCfg.ConversionTeam ?? ConversionTeam,
            ConversionSpeed = newCfg.ConversionSpeed ?? ConversionSpeed,
        };
    }

    IDeclaration IDeclaration.Aggregate(IDeclaration newCfg) =>
        newCfg is ConvertingDilatorDeclaration typed
            ? Aggregate(typed)
            : throw new ArgumentException(
                "The input configuration type does not match the current one!",
                nameof(newCfg)
            );
}

[Translate("converting_dilator", ConceptNames.ConvertingDilator)]
public class ConvertingDilatorDeclarationTranslator
    : ITranslator<ConvertingDilatorDeclaration, ConvertingDilatorDescription>
{
    private readonly DilatorDeclarationTranslator _dilatorDeclarationTranslator = new();

    public ConvertingDilatorDescription ToDescription(
        ConvertingDilatorDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var dilatorDesc = _dilatorDeclarationTranslator.ToDescription(declaration, otherEntities);

        var desc = new ConvertingDilatorDescription()
        {
            Transform = dilatorDesc.Transform,
            Team = dilatorDesc.Team,
            ProduceShips = dilatorDesc.ProduceShips,
            InitialShips = dilatorDesc.InitialShips,
            Capital = dilatorDesc.Capital,
            Garrison = dilatorDesc.Garrison,
            ConversionSpeed = declaration.ConversionSpeed ?? 0,
        };

        if (declaration.ConversionTeam is not null)
            desc.ConversionTeam = otherEntities[declaration.ConversionTeam];

        return desc;
    }
}

[Translate("converting_dilator", ConceptNames.DilatorPreview), OnlyForPreview]
public class ConvertingDilatorPreviewDeclarationTranslator
    : ITranslator<ConvertingDilatorDeclaration, DilatorPreviewDescription>
{
    private readonly DilatorPreviewDeclarationTranslator _dilatorPreviewDeclarationTranslator =
        new();

    public DilatorPreviewDescription ToDescription(
        ConvertingDilatorDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    ) => _dilatorPreviewDeclarationTranslator.ToDescription(declaration, otherEntities);
}
