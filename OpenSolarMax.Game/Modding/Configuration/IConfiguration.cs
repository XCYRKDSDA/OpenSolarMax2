using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Game.Modding.Configuration;

public interface IConfiguration
{
    IConfiguration Aggregate(IConfiguration newCfg);

    IReadOnlyList<string> Requirements => [];

    IDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities, IAssetsManager assets);
}

public interface IConfiguration<out TDesc, TConf> : IConfiguration
    where TDesc : IDescription
    where TConf : IConfiguration<TDesc, TConf>
{
    new TConf Aggregate(TConf newCfg);

    IConfiguration IConfiguration.Aggregate(IConfiguration newCfg) =>
        !newCfg.GetType().IsAssignableTo(typeof(TConf))
            ? throw new ArgumentException("The input configuration type does not match the current one!")
            : Aggregate((TConf)newCfg);

    new TDesc ToDescription(IReadOnlyDictionary<string, Entity> otherEntities, IAssetsManager assets);

    IDescription IConfiguration.ToDescription(
        IReadOnlyDictionary<string, Entity> otherEntities, IAssetsManager assets) =>
        ToDescription(otherEntities, assets);
}
