namespace OpenSolarMax.Mods.Core;

public static partial class Content
{
    public static partial class Textures
    {
        private const string _base = "Textures";

        public static readonly string[] DefaultPlanetTextures;

        public const string DefaultPlanetShape = $"{_base}/PlanetsAtlas.json:PlanetShape";

        private static readonly string[] _defaultPlanetTextureIds =
        {
            "Planet01", "Planet02", "Planet03", "Planet04", "Planet05",
            "Planet06", "Planet07", "Planet08", "Planet09",
        };

        public const string DefaultShip = $"{_base}/ShipAtlas.json:Ship";

        static Textures()
        {
            DefaultPlanetTextures = (from string id in _defaultPlanetTextureIds
                                     select $"{_base}/PlanetsAtlas.json:{id}").ToArray();
        }
    }
}
