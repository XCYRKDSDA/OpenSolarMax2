using Apos.Shapes;
using Nine.Assets;
using Zio;

namespace OpenSolarMax.Game.Assets;

internal class AposShapeFontLoader : IAssetLoader<ShapeFont>
{
    public ShapeFont Load(IFileSystem fs, IAssetsManager assets, in UPath path)
    {
        using var stream = fs.OpenFile(path, FileMode.Open, FileAccess.Read);
        return new ShapeFont(stream);
    }
}
