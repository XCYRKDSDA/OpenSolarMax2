using Microsoft.Extensions.Configuration;

namespace OpenSolarMax.Ecs.Configuration;

public static class ConfigurationHelper
{
    public static T RequireValue<T>(this IConfiguration configuration, string key)
    {
        var section = configuration.GetRequiredSection(key);
        var value = section.Get<T?>();
        return value ?? throw new ArgumentNullException();
    }
}
