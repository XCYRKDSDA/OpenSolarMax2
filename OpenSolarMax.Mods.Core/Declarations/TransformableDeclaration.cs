using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("transformable")]
public class TransformableDeclaration : IDeclaration<TransformableDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public TransformableDeclaration Aggregate(TransformableDeclaration @new)
    {
        return new TransformableDeclaration()
        {
            Parent = @new.Parent ?? Parent,
            Position = @new.Position ?? Position,
            Orbit = Orbit is not null && @new.Orbit is not null
                        ? Orbit.Aggregate(@new.Orbit)
                        : @new.Orbit ?? Orbit,
        };
    }
}

[Translate("transformable", ConceptNames.Transformable)]
public class TransformableDeclarationTranslator : ITranslator<TransformableDeclaration, TransformableDescription>
{
    public TransformableDescription ToDescription(TransformableDeclaration declaration,
                                                  IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (declaration.Parent is null)
        {
            var transform = new AbsoluteTransformOptions();
            if (declaration.Position is { } position)
                transform.Translation = TransformProjection.To3D(position);
            return new TransformableDescription() { Transform = transform };
        }
        else if (declaration.Orbit is null)
        {
            var transform = new RelativeTransformOptions() { Parent = otherEntities[declaration.Parent] };
            if (declaration.Position is { } position)
                transform.Translation = TransformProjection.To3D(position);
            return new TransformableDescription() { Transform = transform };
        }
        else
        {
            if (declaration.Parent is null || declaration.Orbit.Shape is null || declaration.Orbit.Period is null)
                throw new NullReferenceException();

            var revolution = new RevolutionOptions()
            {
                Parent = otherEntities[declaration.Parent],
                Shape = new(declaration.Orbit.Shape.Value.X, declaration.Orbit.Shape.Value.Y),
                Period = declaration.Orbit.Period.Value
            };

            if (declaration.Orbit.Phase is not null)
                revolution.InitPhase = declaration.Orbit.Phase.Value * MathF.PI * 2;

            return new TransformableDescription() { Transform = revolution };
        }
    }
}
