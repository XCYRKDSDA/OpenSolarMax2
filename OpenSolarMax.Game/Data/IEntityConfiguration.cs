using Nine.Assets;
using OpenSolarMax.Game.Utils;

namespace OpenSolarMax.Game.Data;

public interface IEntityConfiguration
{
    // 该接口认为Configuration是只读的，因此不指定ICloneable。是否实现可写和拷贝由子类自行决定

    IEntityConfiguration Aggregate(IEntityConfiguration @new);

    ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets);
}
