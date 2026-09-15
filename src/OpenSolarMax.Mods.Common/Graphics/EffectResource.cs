using System.Reflection;

namespace OpenSolarMax.Mods.Common.Graphics;

internal class EffectResource(string name)
{
    private const string _namespace = "OpenSolarMax.Mods.Common.Graphics";

    public static readonly EffectResource TintEffect = new($"{_namespace}.Tint.mgfxo");

    private readonly object _locker = new();
    private volatile byte[]? _bytecode;

    public byte[] Bytecode
    {
        get
        {
            if (_bytecode is not null)
                return _bytecode;

            lock (_locker)
            {
                if (_bytecode is not null)
                    return _bytecode;

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
