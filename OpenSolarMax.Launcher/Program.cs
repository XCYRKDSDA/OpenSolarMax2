using OpenSolarMax.Game;

namespace OpenSolarMax.Launcher;

internal class Program
{
    static void Main(string[] _)
    {
        using var game = new SolarMax();
        game.Run();
    }
}
