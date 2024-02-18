namespace OpenSolarMax.Game.Data;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ConfiguratorKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
