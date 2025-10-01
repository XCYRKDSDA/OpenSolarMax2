namespace OpenSolarMax.Game.Data;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ConfigurationKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
