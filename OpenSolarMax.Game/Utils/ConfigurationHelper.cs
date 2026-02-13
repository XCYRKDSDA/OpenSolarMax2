using Microsoft.Extensions.Configuration;

namespace OpenSolarMax.Game.Utils;

public static class ConfigurationHelper
{
    public static T RequireValue<T>(this IConfiguration configuration, string key)
        => configuration.GetValue<T?>(key) ?? throw new ArgumentNullException();
}
