using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Game.Modding.Declaration;

public interface IDeclaration
{
    IDeclaration Aggregate(IDeclaration newCfg);

    IReadOnlyList<string> Requirements => [];

    IDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities);
}

public interface IDeclaration<out TDesc, TConf> : IDeclaration
    where TDesc : IDescription
    where TConf : IDeclaration<TDesc, TConf>
{
    new TConf Aggregate(TConf newCfg);

    IDeclaration IDeclaration.Aggregate(IDeclaration newCfg) =>
        !newCfg.GetType().IsAssignableTo(typeof(TConf))
            ? throw new ArgumentException("The input configuration type does not match the current one!")
            : Aggregate((TConf)newCfg);

    new TDesc ToDescription(IReadOnlyDictionary<string, Entity> otherEntities);

    IDescription IDeclaration.ToDescription(IReadOnlyDictionary<string, Entity> otherEntities) =>
        ToDescription(otherEntities);
}
