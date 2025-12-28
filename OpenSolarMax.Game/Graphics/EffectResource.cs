using System.Reflection;

namespace OpenSolarMax.Game.Graphics;

internal class EffectResource(string name)
{
    public static readonly EffectResource ExposureEffect =
        new EffectResource("OpenSolarMax.Game.Graphics.Exposure.mgfxo");

    private readonly object _locker = new();
    private volatile byte[]? _bytecode;

    public byte[] Bytecode
    {
        get
        {
            if (_bytecode is not null) return _bytecode;

            lock (_locker)
            {
                if (_bytecode is not null) return _bytecode;

                _bytecode = PlatformGetBytecode(name);
            }

            return _bytecode;
        }
    }

    private static byte[] PlatformGetBytecode(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(name)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
