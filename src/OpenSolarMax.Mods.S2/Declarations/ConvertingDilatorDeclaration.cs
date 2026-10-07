using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

[SchemaName("converting_dilator")]
public class ConvertingDilatorDeclaration : IDeclaration<ConvertingDilatorDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public string? ConversionTeam { get; set; }

    /// <summary>
    /// 转化波半径的扩张速度
    /// </summary>
    public float? ConversionSpeed { get; set; }

    /// <summary>
    /// 转化波释放后到退出关卡的延迟（秒）
    /// </summary>
    public float? ExitDelay { get; set; }

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
            ConversionTeam = newCfg.ConversionTeam ?? ConversionTeam,
            ConversionSpeed = newCfg.ConversionSpeed ?? ConversionSpeed,
            ExitDelay = newCfg.ExitDelay ?? ExitDelay,
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
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public ConvertingDilatorDescription ToDescription(
        ConvertingDilatorDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new ConvertingDilatorDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.ConversionTeam is not null)
            desc.ConversionTeam = otherEntities[declaration.ConversionTeam];
        if (declaration.ConversionSpeed is not null)
            desc.ConversionSpeed = declaration.ConversionSpeed.Value;
        if (declaration.ExitDelay is float exitDelaySeconds)
            desc.ExitDelay = TimeSpan.FromSeconds(exitDelaySeconds);

        return desc;
    }
}

[Translate("converting_dilator", ConceptNames.DilatorPreview), OnlyForPreview]
public class ConvertingDilatorPreviewDeclarationTranslator
    : ITranslator<ConvertingDilatorDeclaration, DilatorPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public DilatorPreviewDescription ToDescription(
        ConvertingDilatorDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new DilatorPreviewDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        return desc;
    }
}
