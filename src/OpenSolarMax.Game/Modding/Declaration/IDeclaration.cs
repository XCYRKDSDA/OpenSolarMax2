namespace OpenSolarMax.Game.Modding.Declaration;

public interface IDeclaration
{
    IDeclaration Aggregate(IDeclaration newCfg);

    IReadOnlyList<string> Requirements => [];
}

public interface IDeclaration<TConf> : IDeclaration
    where TConf : IDeclaration<TConf>
{
    new TConf Aggregate(TConf newCfg);

    IDeclaration IDeclaration.Aggregate(IDeclaration newCfg) =>
        !newCfg.GetType().IsAssignableTo(typeof(TConf))
            ? throw new ArgumentException(
                "The input configuration type does not match the current one!"
            )
            : Aggregate((TConf)newCfg);
}
