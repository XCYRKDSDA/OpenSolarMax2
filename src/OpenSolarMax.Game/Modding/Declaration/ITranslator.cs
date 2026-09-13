using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Game.Modding.Declaration;

public interface ITranslator
{
    IDescription ToDescription(
        IDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    );
}

public interface ITranslator<in TDecl, out TDesc> : ITranslator
    where TDecl : IDeclaration<TDecl>
    where TDesc : IDescription
{
    TDesc ToDescription(TDecl declaration, IReadOnlyDictionary<string, Entity> otherEntities);

    IDescription ITranslator.ToDescription(
        IDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration is not TDecl typedDeclaration)
            throw new ArgumentException("Declaration type not match!", nameof(declaration));
        return ToDescription(typedDeclaration, otherEntities);
    }
}
