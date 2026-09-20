using System.Reflection;

namespace OpenSolarMax.Mods.S2.Graphics;

internal class EffectResource(string name)
{
    private const string _namespace = "OpenSolarMax.Mods.S2.Graphics";

    public static readonly EffectResource TintEffect = new($"{_namespace}.Tint.mgfxo");
    public static readonly EffectResource BoxEffect = new($"{_namespace}.Box.mgfxo");
    public static readonly EffectResource CircleEffect = new($"{_namespace}.Circle.mgfxo");
    public static readonly EffectResource GlowCircleEffect = new($"{_namespace}.GlowCircle.mgfxo");
    public static readonly EffectResource RingEffect = new($"{_namespace}.Ring.mgfxo");
    public static readonly EffectResource SegmentEffect = new($"{_namespace}.Segment.mgfxo");

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
