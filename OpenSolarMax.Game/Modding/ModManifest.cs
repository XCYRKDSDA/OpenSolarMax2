namespace OpenSolarMax.Game.Modding;

internal enum ModType
{
    Behavior,
    Content
}

internal class ModManifest
{
    public ModType Type { get; set; }

    public string Name { get; set; }

    public string Author { get; set; }

    public string Version { get; set; }

    public string Description { get; set; }

    public string Link { get; set; }

    public string UUID { get; set; }

    public string Assembly { get; set; }

    public string Content { get; set; }
}
