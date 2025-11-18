namespace OpenSolarMax.Game.Utils;

public class FmodException(FMOD.RESULT err) : Exception
{
    public static void Check(FMOD.RESULT code)
    {
        if (code == FMOD.RESULT.OK) return;
        throw new FmodException(code);
    }
}
