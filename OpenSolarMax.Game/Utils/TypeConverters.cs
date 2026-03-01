using System.ComponentModel;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Utils;

public class ColorConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string str) return base.ConvertFrom(context, culture, value);

        if (str[0] == '#')
        {
            var r = byte.Parse(str.Substring(1, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(3, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(5, 2), NumberStyles.HexNumber);
            var a = str.Length > 7 ? byte.Parse(str.Substring(7, 2), NumberStyles.HexNumber) : (byte)255;
            return new Color(r, g, b, a);
        }
        else
        {
            var parts = str.Split('*', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sysColor = System.Drawing.Color.FromName(parts[0]);
            var xnaColor = new Color(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
            if (parts.Length > 1)
                xnaColor *= float.Parse(parts[1]);
            return xnaColor;
        }
    }
}
