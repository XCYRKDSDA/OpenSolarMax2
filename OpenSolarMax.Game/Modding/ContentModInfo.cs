using Zio;

namespace OpenSolarMax.Game.Modding;

internal record ContentModInfo : CommonModInfo
{
    public required DirectoryEntry Content { get; init; }
}
