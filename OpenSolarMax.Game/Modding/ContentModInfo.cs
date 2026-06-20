using Zio;

namespace OpenSolarMax.Game.Modding;

internal record ContentModInfo(
    DirectoryEntry Directory,
    string FullName,
    string ShortName,
    FileEntry? Preview,
    FileEntry? Background,
    string Author,
    string Version,
    string Description,
    string Link,
    DirectoryEntry Content
) : ICommonModInfo;
