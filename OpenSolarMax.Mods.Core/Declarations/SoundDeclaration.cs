using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("sound")]
public class SoundDeclaration : IDeclaration<SoundDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Sound { get; set; }

    public SoundDeclaration Aggregate(SoundDeclaration newCfg)
    {
        return new SoundDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Sound = newCfg.Sound ?? Sound,
        };
    }
}

[Translate("sound", ConceptNames.SimpleSound)]
public class SoundDeclarationTranslator : ITranslator<SoundDeclaration, SimpleSoundDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public SimpleSoundDescription ToDescription(SoundDeclaration declaration,
                                                IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (string.IsNullOrEmpty(declaration.Sound)) throw new NullReferenceException();

        var desc = new SimpleSoundDescription()
        {
            SoundEffect = declaration.Sound,
        };

        var tfCfg = new TransformableDeclaration()
            { Parent = declaration.Parent, Position = declaration.Position, Orbit = declaration.Orbit };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        return desc;
    }
}
